using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using Flagship_Manager_Dev.Objects;
using System.Diagnostics;
using System.Net;
//using System.Runtime.Serialization.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
//using Newtonsoft.Json;
using System.Web;

namespace FlagShip_Manager
{
    internal class jobManager
    {
        //public static List<Job> jobArchive = new List<Job>();
        public static List<Job> jobList = new List<Job>();
        public static List<int> ActiveIDList = new List<int>();
        public static List<int> ArchiveIDList = new List<int>();
        private static DateTime LastCleanup = DateTime.MinValue;
        public static bool clearAvailableWorkers = false;
        public static Path_Settings ActiveSettings = new Path_Settings();
        public static bool Setup = false;
        public static bool wait = false;

        public static void Manager()
        {
            string hostName = Dns.GetHostName();
            string IP = Dns.GetHostByName(hostName).AddressList[1].ToString();
            string ctlPath = "";
            Process.Start("explorer", $"http://{IP}:8008");
            while (!Setup)
            {
                if (!wait) Setup = ActiveSettings.Load();
                Thread.Sleep(1000);
            }
            ctlPath = ActiveSettings.CtlFolder;
            if(!Directory.Exists($"{ctlPath}\\Archive")) 
            { 
                Directory.CreateDirectory($"{ctlPath}\\Archive");
            }
            Thread dbc = new Thread(() => Database.DataBaseManager());
            Thread cfnj = new Thread(() => checkForNewJobs(ctlPath));
            Thread jd = new Thread(() => jobDirector());
            Thread Progress = new Thread(() => CheckActiveJobsProgress());
            //if(Database.ImportSettings())
            
            dbc.Start(); //Run the database Maanager to control loading and saving of database file.
            
            Thread.Sleep(2000);

            Progress.Start();//Checks progress on active Jobs and thier render tasks.

            cfnj.Start();//Thread that checks for new jobs in the control folder 
                         // This is where I can later add network code to transfer the Jobs through TCP rather than watch folders.

            jd.Start(); //Job director imports new jobs and sends them to workers. 

        }
        private static void jobDirector()
        {
            List<Job> activeJobs = new List<Job>();
            List<int> availableWorkers = new List<int>();
            ref List<WorkerObject> workers = ref WorkerServer.WorkerList;

            bool clear = true;

            while (true)
            {
                //workers = WorkerServer.WorkerList;
                if (LastCleanup < DateTime.Now)
                {
                    CleanJobList();
                    LastCleanup = DateTime.Now.AddHours(12);
                }
                else if (LastCleanup == DateTime.MinValue) LastCleanup = DateTime.Now;

                Thread.Sleep(1000);
                activeJobs.Clear();
                availableWorkers.Clear();

                if (jobList.Count() > 0 && !Database.Startup)
                {

                    activeJobs = Logic.getQueuedJobs(jobList);
                    if (activeJobs.Count == 0)
                    {
                        if (clear)
                        {
                            clear = false;
                            Thread.Sleep(1000);
                            Console.Clear();
                            Readout.ReadoutBuffer = "Awaiting new render Jobs.";
                        }
                    }
                    else
                    {
                        Readout.ReadoutBuffer = "Working on Jobs...";
                    }
                    if (clearAvailableWorkers) clearAvailableWorkers = false;
                    foreach (Job _job in activeJobs)
                    {
                        int TaskIndex = 0;
                        availableWorkers = Logic.getAvailableWorkers(_job);
                        if (availableWorkers.Count() < 1) continue;

                        if (clearAvailableWorkers) break;
                        foreach (int wi in availableWorkers)
                        {
                            if (clearAvailableWorkers) break;
                            foreach (renderTask t in _job.renderTasks)
                            {
                                TaskIndex = _job.renderTasks.FindIndex(i => i == t) + 1;
                                if (t.Status == 0)
                                {//task status. ready for work(0), rendering(1), complete(2), task pending(3), failed(4), canceled(5) starting up(6), offline(7), passive(8) asleep(9).
                                    if (t.Status == 2)
                                    {
                                        continue;
                                    }
                                    WorkerObject worker = workers[wi];
                                    if (worker.AvailableApps.Any(a => a.AppName == _job.RenderApp))
                                    {
                                        try
                                        {
                                            //int LogIndex = t.taskLogs.Attempt();
                                            worker.lastSubmittion = DateTime.Now;
                                            if (_job.Status == 0)
                                            {
                                                _job.StartTimes.Add(DateTime.Now);
                                            }
                                            //if (!Drives.Contains(Directory.GetDirectoryRoot(_job.outputDir)))
                                            t.taskLogs.add();
                                            sendTasktoClientBuffer(_job, t, ref worker);
                                            t.taskLogs.WriteToWorker($" Task {TaskIndex} submitted to {worker.name} for rendering.\n------------------------------Worker Log start------------------------------\n", false);

                                            if (_job.Status != 1)
                                            {
                                                _job.Status = 1;
                                                //_job.started = true;
                                                //_job.StartTimes = DateTime.Now;
                                            }
                                            t.Status = 1;
                                            t.taskLogs.SubmitTime[t.Attempt()] = DateTime.Now;

                                            //t.Worker[LogIndex] = worker.name;
                                            
                                            
                                            t.taskLogs.WriteID(worker.WorkerID);
                                            if (clearAvailableWorkers) break;
                                            worker.ConsoleBuffer = ($"Sending: {_job.Name} task index: {_job.renderTasks.FindIndex(i => i == t) + 1} of {_job.renderTasks.Count()} to {workers[wi].name}");
                                            worker.awaitUpdate = true;
                                            break;
                                        }
                                        catch
                                        {
                                            worker.renderTaskID = 0;
                                            worker.JobID = 0;
                                            worker.ConsoleBuffer = $"Failed to send task to {worker.name} buffer";
                                            t.Status = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else Console.WriteLine("No jobs in queue.");
            }
        }
        private static void CleanJobList()
        {
            for (int ID = 0; ID < ArchiveIDList.Count(); ID++)
            {
                try
                {
                    int tempID = ArchiveIDList[ID];
                    Job tempJob = jobList.Find(j => j.ID == tempID);
                    if (tempJob != null)
                    {
                        if (tempJob.ID != -1)
                        {
                            if (tempJob.ArchiveDate.AddDays(7) < DateTime.Now)
                            {
                                try
                                {
                                    File.Delete(tempJob.Project);
                                    File.Delete($"{ActiveSettings.CtlFolder}\\Archive\\{tempJob.Name}.txt");
                                }
                                catch
                                {
                                    Console.WriteLine("Cannot remove project file. it will remain on server.");
                                }
                                jobList.Remove(tempJob);
                                ArchiveIDList.Remove(tempID);
                                ID--;
                            }

                        }
                        else
                        {
                            ArchiveIDList.Remove(tempID);
                            ID--;
                        }
                    }
                    else
                    {
                        ArchiveIDList.Remove(tempID);
                        ID--;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }       
        private static void sendTasktoClientBuffer(Job j, renderTask t, ref WorkerObject c)
        {
            //WorkerObject worker = WorkerServer.WorkerList.Find(wi => wi == c);
            tcpPacket sendPacket = new tcpPacket();
            sendPacket.arguments = new string[11];
            sendPacket.command = "render";

            //Aruguments RenderType(0) Project Filepath(1), ShotName(2), Output Type(3), output Filepath(4),
            //first Frame(5), final Frame(6), Overwrite(7), Video(8), Frame Step(9), Queue Index(10)
            sendPacket.arguments[1] = j.Project;
            sendPacket.arguments[2] = j.Name;
            sendPacket.arguments[3] = j.RenderPreset;
            sendPacket.arguments[4] = j.outputPath;
            if (t.adjustedFirstFrame > t.FirstFrame && !j.vid) sendPacket.arguments[5] = t.adjustedFirstFrame.ToString();
            else sendPacket.arguments[5] = t.FirstFrame.ToString();
            sendPacket.arguments[6] = t.finalFrame.ToString();
            sendPacket.arguments[7] = j.Overwrite.ToString();
            sendPacket.arguments[8] = j.vid.ToString();
            sendPacket.arguments[9] = j.FrameStep.ToString();
            sendPacket.arguments[10] = j.QueueIndex.ToString();


            //This will chose which app the worker will use to render the job.
            if (j.RenderApp.ToLower() == "ae")
            {
                sendPacket.arguments[0] = "ae";//$"-project \"argument[1]}\" -comp \"{argument[2]}\" -OMtemplate \"{argument[3]}\" -output \"{[argument[4]}\" -s {argument[5]} -e {argument[6]}";
            }
            else if (j.RenderApp.ToLower() == "blender")
            {
                sendPacket.arguments[0] = "blender";//$"-b \"{args[1]}\" -s {args[5]} -e {args[6]} -a";
            }
            else if (j.RenderApp.ToLower() == "fusion")
            {
                sendPacket.arguments[0] = "fusion";//$"\"{args[1]}\" -render -start {args[5]} -end {args[6]}"
            }
            else
            {//Failled to catagorize render type.
                //It seems Incredably unlikely that we could ever get to this point without something else breaking.
            }
            c.renderTaskID = t.ID;
            c.JobID = j.ID;
            c.packetBuffer = sendPacket;
            c.Status = 1;
        }
        private static void checkForNewJobs(string path)
        {
            Regex ErrorCheck = new Regex(@"(ERROR)");
            Readout.ReadoutBuffer = "Started checking watch folder for new jobs.";

            while (true)
            {
                Thread.Sleep(1000);

                try
                {
                    string[] newFiles = Directory.GetFiles(path);
                    if (newFiles.Length > 0)
                    {
                        Parallel.ForEach(newFiles, _job =>
                        {
                            if (File.Exists(_job))
                            {
                                if (!ErrorCheck.IsMatch(_job))
                                {
                                    ImportJob(_job); //creates Job object from Job file.
                                }
                                else { Console.WriteLine("Failed error check."); }
                                
                            }

                        });
                        
                    }
                }
                catch
                {
                    Readout.ReadoutBuffer = $"Unable to read from {path}.";
                }
            }
            Console.WriteLine("No idea how we got here");

        }
        private static void ImportJob(string filePath)
        {
            List<importRender> Imports = new List<importRender>();
            Random RNG = new Random();
            Job newJob = new Job();
            bool[] inQueue = null;
            int count = 0;
            using (var Stream = new StreamReader(filePath))
            {
                Imports = JsonSerializer.Deserialize<List<importRender>>(Stream.ReadToEnd());
            }
            inQueue = new bool[Imports.Count];
            foreach (var Import in Imports)
            {
                int TotalFrames = Import.FrameRange;
                int split = Import.split;
                int chunk = 0;
                if (split == 0) //This will auto adjust frame split size to try and get each chunk to between 10 and 50 frames. This should allow the shot to be rendered over a good number of computers but no so many that it causes problems.
                {
                    split = 2;
                    while (true)
                    {
                        chunk = TotalFrames / split;
                        if (chunk > 35) split++;
                        else if (chunk < 7)
                        {
                            split--;
                            break;
                        }
                        else break;
                    }
                }
                newJob = new Job();
                //string test = Import.Filepath;
                if (Import.Name == "") newJob.Name = "Unsaved Project";
                else newJob.Name = Import.Name;
                
                newJob.Project = Path.GetFullPath(HttpUtility.UrlDecode(Import.Project));
                newJob.WorkingProject = Import.WorkingProject;
                newJob.outputPath = Import.Filepath;
                if (Import.Filepath != "") newJob.outputDir = Directory.GetParent(Import.Filepath).ToString();
                else newJob.outputDir = "";
                newJob.RenderPreset = Import.outputType;
                newJob.FirstFrame = Convert.ToInt32(Math.Floor(Import.StartFrame));
                //newJob.FirstFrame = Import.StartFrame;
                if (Import.FrameStep > 1)
                {
                    newJob.FrameStep = Import.FrameStep;
                    newJob.TotalFramesToRender = Convert.ToInt32(decimal.Floor(Import.FrameRange / Import.FrameStep));
                    newJob.FrameRange = newJob.TotalFramesToRender * Import.FrameStep;
                }
                else
                {
                    newJob.FrameStep = 1;
                    newJob.TotalFramesToRender = Import.FrameRange;
                    newJob.FrameRange = Import.FrameRange;
                }
                newJob.GPU = Convert.ToBoolean(Import.GPU);
                newJob.FileFormat = Logic.GetRenderType(Import.outputType, Import.Filepath);
                if (Import.RenderApp.ToLower() == "blender") newJob.Overwrite = true;
                else newJob.Overwrite = Convert.ToBoolean(Import.OW);
                newJob.QueueIndex = Import.QueueIndex;
                newJob.vid = Import.vid;
                newJob.ID = RNG.Next(1000000, 9999999);
                while (true)
                {
                    if (jobList.Any(j => j.ID == newJob.ID))
                    {
                        newJob.ID = RNG.Next(1000000, 9999999);
                    }
                    else break;
                }
                newJob.WorkerBlackList = new List<int>();
                newJob.RenderApp = Import.RenderApp.ToLower();
                newJob.Status = 0;
                newJob.CreationTime = DateTime.Now;
                newJob.renderTasks = GenerateSteppedTasks(newJob.FirstFrame, Import.FrameRange, split, newJob.CreationTime, newJob.vid, newJob.FrameStep, newJob.ID);
                newJob.ProgressPerFrame = 100/(float)newJob.TotalFramesToRender;
                //else newJob.renderTasks = GenerateTasks(newJob.FirstFrame, Import.FrameRange, split, newJob.CreationTime, newJob.vid);
                //newJob.TotalCompletedFrames = 0;
                //if(newJob.FrameStep > 1)
                /*if (newJob.renderTasks.Count() < 8)
                {
                    newJob.TaskArrow = "Images/minus.png";
                    newJob.ExtendedHeight = "auto";
                }
                else newJob.TaskArrow = "Images/arrow down.png";
                */
                //newJob.ConsoleBuffer = $"{newJob.Name} Has been added to the queue and is awaiting available workers.";
                //newJob.started = false;
                //newJob.BuildStatusThread();
                if (newJob.renderTasks.Count < 1)
                {
                    Readout.ReadoutBuffer = "Could not import Job";
                    File.Move(filePath, filePath + ".ERROR");
                    return;
                }
                newJob.Priority = Import.Priority;
                newJob.ProgressPerSecond = new List<double>();
                newJob.BuiildOutputDir();
                newJob.SetOutputOffset();
                jobList.Add(newJob);
                ActiveIDList.Add(newJob.ID);
                if (jobList.Contains(newJob))
                {
                    inQueue[count] = true;
                    count++;
                }
                else
                {
                    Readout.ReadoutBuffer = "Could not import Job";
                }
            }
            string ArchiveFilePath = Path.GetDirectoryName(filePath) + "\\Archive\\" + newJob.Name + ".txt";
            //int Count = 1;
            while (true)
            {
                if (!File.Exists(ArchiveFilePath)) break;
                else
                {
                    //ount++;
                    ArchiveFilePath = Path.GetDirectoryName(filePath) + "\\Archive\\" + newJob.Name + "_" + RNG.Next(9999) + ".txt";
                }
            }
            if (inQueue.Contains(false)) return;
            else
            {
                try
                {
                    Thread.Sleep(RNG.Next(200));
                    File.Move(filePath, ArchiveFilePath);//File.Delete(filePath);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                }
                
            }
            Database.UpdateDBFile = true;

        }
        private static List<renderTask> GenerateSteppedTasks(int start, int range, int split, DateTime _start, bool _vid, int Step, int _JID) //This is the ONLY place to adjust frame numbers, NEVER DO IT ANYWHERE ELSE.
        {
            if (_vid) split = 1;
            Random rnd = new Random();
            List<renderTask> _return = new List<renderTask>();
            //List<int> TempFrameNumbers = new List<int>();
            decimal AdjustedRange = decimal.Floor(range / Step);
            decimal roundSplitRange = decimal.Floor(AdjustedRange / split);

            int differance = Convert.ToInt32(AdjustedRange) - (Convert.ToInt32(roundSplitRange) * split);
            int firstFrame = start;
            int frameRange = 0;
            int AdjustedFrameCount = 0;
            //int First = start;
            //bool finalDifferanceTask = false;

            for (int c = 0; c < split; c++)
            {
                renderTask nt = new renderTask();
                //frameRange = Convert.ToInt32(decimal.Floor(roundSplitRange)*Step);
                AdjustedFrameCount = Convert.ToInt32(roundSplitRange);
                nt.FirstFrame = firstFrame;
                nt.adjustedFirstFrame = firstFrame;

                if (differance > 0)
                {
                    AdjustedFrameCount++;
                    differance--;
                }
                nt.adjustedFrameRange = AdjustedFrameCount;
                frameRange = (AdjustedFrameCount * Step);
                if (c == 0)
                {
                    nt.finalFrame = firstFrame + frameRange - Step;
                    firstFrame += (AdjustedFrameCount * Step);//This just removes the 1 that we started with.
                }
                else
                {
                    nt.finalFrame = firstFrame + frameRange - Step;
                    firstFrame += (AdjustedFrameCount * Step);
                }
                //int countFrames = nt.FirstFrame;

                nt.GenerateFrameCount(Step);
                //nt.finished = false;
                nt.ID = rnd.Next(1000000, 9999999);
                nt.JID = _JID;
                //nt.SubmitTime = new DateTime[5];
                //for (int i = 0; i < 5; i++) nt.SubmitTime[i] = _start;
                nt.finishTime = DateTime.MinValue;
                //nt.Worker = new string[5];
                //nt.Log = new string[5];
                nt.Status = 0;
                nt.ProgressPerFrame = 100 / (float)nt.RenderFrameNumbers.Count();

                //for (int i = 0; i < 5; i++) nt.Log[i] = "";
                int attempt = 0;
                while (true)
                {
                    try
                    {
                        if (jobList.Any(j => j.renderTasks.Any(rt => rt.ID == nt.ID))) nt.ID = rnd.Next(1000000, 9999999);
                        else break;
                    }
                    catch
                    {
                        attempt++;
                        if (attempt > 10) return new List<renderTask>();
                    }
                }
                //nt.RenderFrameNumbers = TempFrameNumbers.ToArray();
                //TempFrameNumbers.Clear();
                _return.Add(nt);
            }
            return _return;
        }
        private static void CheckActiveJobsProgress()
        {
            List<string> ProgressThreads = new List<string>();
            while (true)
            {
                for (int ji = 0; ji < jobList.Count(); ji++)
                {
                    Job j = jobList[ji];
                    if (j.Status == 1 && !ProgressThreads.Any(t => t == j.Name))
                    {
                        Thread temp = new Thread(() => {
                            try
                            {
                                while (j.Status == 1)
                                {
                                    j.newProgress();
                                    j.TimeEstimate();
                                    if (j.RenderApp == "ae") Thread.Sleep(1000);
                                    else Thread.Sleep(10000);
                                }
                                ProgressThreads.Remove(j.Name);
                            }
                            catch(Exception EX)
                            {
                                
                                j.fail = true;
                                foreach(renderTask r in j.renderTasks)
                                {
                                    r.taskFail("Failed to start progress counter.", true);
                                    r.taskLogs.WriteToManager(EX.ToString());
                                }
                            }
                        });
                        ProgressThreads.Add(j.Name);
                        temp.Start();
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
/*
 * internal static void removeTromBlacklist(Job j)
{
    int respInt = -1;
    if (j.WorkerBlackList.Count() < 1)
    {
        Console.WriteLine("No workers in job blacklist");
        return;
    }
    Console.WriteLine("Please select an index for the worker you would like to remove.");
    int Count = 0;
    foreach (var w in j.WorkerBlackList)
    {
        Console.WriteLine($"{Count}: {w}");
        Count++;
    }
    Console.WriteLine($"{Count + 1}: Clear all.");
    try
    {
        respInt = Convert.ToInt32(Console.ReadLine());
        if (respInt <= Count + 1 && respInt >= 0)
        {
            if (respInt == Count + 1)
            {
                j.WorkerBlackList.Clear();
            }
            else j.WorkerBlackList.RemoveAt(respInt);
        }
        else
        {
            Console.Write($"You entered {respInt} which is not within the number range of 0 to {Count}. Returning you to the main console.");
        }
    }
    catch
    {
        Console.WriteLine("You did not enter a valid number. Returning you to main console.");
    }


}
internal static void addToBlacklist(Job j)
{
    int respInt = -1;
    var Cl = WorkerServer.WorkerList;
    int Count = 0;
    if (Cl.Count() < 1)
    {
        Console.WriteLine("No workers to add to blacklist.");
        return;
    }
    Console.WriteLine("Please enter the index for the worker you would like to add.");
    foreach (var w in Cl)
    {
        Console.WriteLine($"{Count}: {w.name}");
        Count++;
    }

    try
    {
        respInt = Convert.ToInt32(Console.ReadLine());
        if (respInt <= Count && respInt >= 0)
        {

            if (j.renderTasks.Any(rt => rt.ID == Cl[respInt].renderTaskID)) WorkerServer.cancelWorker(Cl[respInt], false, true);
            else j.WorkerBlackList.Add(Cl[respInt].WorkerID);
        }
        else
        {
            Console.Write($"You entered {respInt} which is not within the number range of 0 to {Count}. Returning you to the main console.");
        }
    }
    catch
    {
        Console.WriteLine("You did not enter a valid number. Returning you to main console.");
    }

}
internal static void restartJob(Job j)
{
    foreach (var rt in j.renderTasks)
    {
        if (rt.Status == 1)
        {
            WorkerServer.cancelWorker(WorkerServer.WorkerList[Logic.matchTasktoWorker(rt)], false, false);
        }
        rt.Status = 0;
        rt.progress = 0;
        //rt.SubmitTime = new DateTime[5];
        rt.taskLogs.ArhiveAndClear();
        //rt.finished = false;
        //for (int i = 0; i < 5; i++) rt.Worker[i] = "";
    }
    j.ConsoleBuffer = $"{j.Name} Has been added to the queue and is awaiting available workers.";
    //j.started = false;
    j.RemainingTime = new TimeSpan();
    j.ProgressPerSecond.Clear();
    j.Status = 0;
    j.StartTimes = new List<DateTime>();
    j.EndTimes = new List<DateTime>();
    j.Progress = 0;
    j.fail = false;
    j.finished = false;
    j.WorkerBlackList.Clear();
}
public static bool CheckFiles(Job _j, renderTask _rT, bool DBLoad = false)
{
    Regex numPattern = new Regex(@"(\[#+\])");
    int LogIndex = _rT.Attempt();

    string outputDir = _j.outputDir;
    string extention = Path.GetExtension(_j.outputPath).ToLower();
    string fileName = Path.GetFileNameWithoutExtension(_j.outputPath).ToLower();
    string filePath = $@"{outputDir}\";

    string Padding = "";
    //int currentFrame = 0;
    //int AdjTaskframes = _rT.adjustedFrameRange;//FindAdjustedRange((_rT.finalFrame - _rT.FirstFrame), _j.FrameStep);
    double taskPercnt = 100 / _rT.adjustedFrameRange; //Convert.ToDouble((_rT.finalFrame - _rT.FirstFrame) + 1); ; 
    double JobPercet = 100 / Convert.ToDouble(FindAdjustedRange(_j.TotalFramesToRender, _j.FrameStep));

    //TimeSpan timeOffset = TimeSpan.Zero;


    if (_j.vid)
    {
        return FileCheck.CheckFileReadability(_j.outputPath);
    }
    bool appendtoEnd = false;
    int paddingCount = 0;
    int Step = _j.FrameStep;
    string match = "";
    Regex NumReplacePatern = new Regex("");
    if (_j.RenderApp.ToLower() == "ae")
    {
        NumReplacePatern = new Regex(@"(\[#+\])");
        paddingCount = Regex.Match(fileName, @"#+(?=])").Length;
        match = NumReplacePatern.Matches(fileName).First().Value;
        //replaceString = PoundString
    }
    else if (_j.RenderApp.ToLower() == "fusion")
    {
        NumReplacePatern = new Regex(@"[0-9]+$");
        paddingCount = NumReplacePatern.Match(fileName).Length;
        match = NumReplacePatern.Match(fileName).Value;
    }
    else if (_j.RenderApp.ToLower() == "blender")
    {
        NumReplacePatern = new Regex(@"(#+)");
        paddingCount = NumReplacePatern.Match(fileName).Length;
        match = NumReplacePatern.Matches(fileName).Last().Value;
    }
    else return false;
    if (match == "") appendtoEnd = true;
    //currentFrame = _rT.FirstFrame;
    if (_rT.RenderFrameNumbers.Count() == 0) _rT.GenerateFrameCount(Step);
    List<int> UnfinishedFrames = new List<int>();
    foreach (int frame in _rT.RenderFrameNumbers)//Creates a list of unfinished fraems;
    {
        if (!_rT.FinishedFrameNumbers.Any(f => f == frame))
        {
            UnfinishedFrames.Add(frame);
        }
    }
    int completedFrames = _rT.FinishedFrameCount;
    bool done = true;
    foreach (var frameNum in UnfinishedFrames)
    {
        string filePathwithNum = "";
        DateTime fileAge = new DateTime();

        //int TempFrame = _rT.RenderFrameNumbers[i];//_rT.FirstFrame + (Step * i);

        Padding = "";
        if (appendtoEnd)
        {
            for (int b = 0; b < (4 - frameNum.ToString().Length); b++) Padding += "0";
            filePathwithNum = $"{filePath}{fileName}{Padding}{frameNum}{extention}";
        }
        else
        {

            for (int b = 0; b < (paddingCount - frameNum.ToString().Length); b++) Padding += "0";
            filePathwithNum = $"{filePath}{fileName.Replace(match, Padding + frameNum)}{extention}";
        }
        if (File.Exists(filePathwithNum))
        {
            fileAge = File.GetLastWriteTime(filePathwithNum) - _j.OutputDeviceOffset;
            double size = 0;
            while (size == 0)
            {
                try
                {
                    size = new FileInfo(filePathwithNum).Length;
                }
                catch
                {
                    _rT.taskLogs.WriteToManager($"Failed to get file size on frame: {frameNum}");
                }
            }
            if ((_j.Overwrite && _j.CreationTime < fileAge) || !_j.Overwrite)
            {
                if (size >= _j.SmallestFile && _j.SmallestFile > 0)
                {
                    //File is assumed good by comparing the smallest known good file to this file's size.  
                    completedFrames++;
                    _rT.FinishedFrameNumbers.Add(frameNum);
                    _rT.taskLogs.WriteToManager($"{frameNum} Found, assumed good by size. \n");
                }
                else if (FileCheck.CheckFileReadability(filePathwithNum))
                {
                    //File failed file size check, checking intergrity explicetly. This is FAR more disk intensive and would slow everything down consiterably if done to each file individually.
                    //Every task has a smallest file which is used for file check, if a file is smaller than the samllest its checked explicitly, if found good its size is used as the new smallest. 
                    //if (_rT.SmallestFile > size || _rT.SmallestFile == -1)
                    //{
                    _rT.taskLogs.WriteToManager($"{frameNum} Found, checked explicitly\n");

                    if (size > 1024)
                    {
                        _j.SmallestFile = Convert.ToDouble(size);
                        _rT.taskLogs.WriteToManager($"Smallest file size set to: {Math.Round(size / 1024)}KB\n");
                    }


                    //}

                    completedFrames++;
                    //_rT.adjustedFirstFrame = frameNum;
                    _rT.FinishedFrameNumbers.Add(frameNum);
                    //_rT.mangagerLog[_rT.attempt] += $"{frameNum} Found, checked explicitly\n";

                }
                else
                {
                    _rT.taskLogs.WriteToManager($"{frameNum} Found, but failed integreity check.\n");

                }
            }
        }
        else//File dosen't exist.
        {
            done = false;
            //Console.WriteLine($"{filePathwithNum} Does NOT exit.");
        }

    }

    _rT.FinishedFrameNumbers.Sort();
    if (_rT.FinishedFrameNumbers.Count() > 0) _rT.adjustedFirstFrame = _rT.FinishedFrameNumbers.Last();

    _rT.progress = Math.Ceiling(completedFrames * taskPercnt); ;
    _rT.FinishedFrameCount = completedFrames;

    if (_rT.adjustedFrameRange == _rT.FinishedFrameNumbers.Count()) return true;
    else return false;
}
private static void CheckTimeOffset(string _outputDir) //Checks time offset of the output server. This will make sure nothing breaks when the file server is set to a diferent time than the manager.
{
    DateTime Now = DateTime.Now;
    string Root = Path.GetPathRoot(_outputDir);
    if (Drives.Contains(Root)) return;
    string timeTestFile = Root + "TimeTest.txt";
    try
    {
        File.WriteAllText(timeTestFile, _outputDir);
    }
    catch
    {
        Console.WriteLine("Unable to write time test file");
        return;
    }

    var creationTime = File.GetLastWriteTime(timeTestFile);

    TimeSpan Offset = Now - creationTime;
    if (creationTime > DateTime.Now) Offset *= -1;
    var timeDIfferance = Now - (creationTime + Offset);
    if (timeDIfferance < TimeSpan.FromSeconds(1))
    {
        Console.WriteLine("Time synced to " + Root);
        DriveTimeOffset.Add(Offset);
        Drives.Add(Root);
    }
    else
    {
        DriveTimeOffset.Add(TimeSpan.Zero);
        Drives.Add(Root);
    }
    Thread.Sleep(1000);
    File.Delete(timeTestFile);

}
*/