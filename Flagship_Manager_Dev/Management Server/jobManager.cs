using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace FlagShip_Manager
{
    internal class jobManager
    {
        //Stores and imports Jobs, manages RenderTasks distrobution, also monitors job progress and estimates time.
        //TODO: Breakup into new objects. 

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
            //Startup thread.
            //loads database.
            //Starts Database Manager thread.
            //Starts Job impoort thread.
            //Starts Job Director thread.
            //Starts Progress monitor thread. 

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
            
            dbc.Start(); //Run the database Maanager to control loading and saving of database file.
            
            Thread.Sleep(2000);

            Progress.Start();//Checks progress on active Jobs and thier render tasks.

            cfnj.Start();//Thread that checks for new jobs in the control folder 

            jd.Start(); //Job director imports new jobs and sends them to workers. 

        }
        private static void jobDirector()
        {
            //Manages renderTask Distrobution to workers. 
            //TODO: Swap out Lists for stacks or Queues.  

            List<Job> activeJobs = new List<Job>();
            List<int> availableWorkers = new List<int>();
            ref List<WorkerObject> workers = ref WorkerServer.WorkerList;

            bool clear = true;

            while (true)
            {
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
                                {
                                    //Worker status map:
                                    //0 = ready for work
                                    //1 = rendering
                                    //2 = complete
                                    //3 = task pending
                                    //4 = failed
                                    //5 = canceled
                                    //6 = starting up
                                    //7 = offline
                                    //8 = passive
                                    //9 = asleep

                                    if (t.Status == 2)
                                    {
                                        continue;
                                    }
                                    WorkerObject worker = workers[wi];
                                    if (worker.AvailableApps.Any(a => a.AppName == _job.RenderApp))
                                    {
                                        try
                                        {
                                            worker.lastSubmittion = DateTime.Now;
                                            if (_job.Status == 0)
                                            {
                                                _job.StartTimes.Add(DateTime.Now);
                                            }
                                            t.taskLogs.add();
                                            sendTasktoClientBuffer(_job, t, ref worker);
                                            t.taskLogs.WriteToWorker($" Task {TaskIndex} submitted to {worker.name} for rendering.\n------------------------------Worker Log start------------------------------\n", false);
                                            _job.Status = 1;
                                            t.Status = 1;
                                            t.taskLogs.SubmitTime[t.Attempt()] = DateTime.Now;
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
                //Removes Jobs after they have spent 7 days in archie. 
                //TODO: Make auto archive optional and add UI for controlling how often it happens.

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
            //Prepes a tcpPacket to send renderTask to worker
            //TODO: add build function to tcpPacket and rewrite code to be more readable.

            tcpPacket sendPacket = new tcpPacket();
            sendPacket.arguments = new string[11];
            sendPacket.command = "render";
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
                sendPacket.arguments[0] = "ae";
                //AfterEffects Arguments breakdown: $"-project \"argument[1]}\" -comp \"{argument[2]}\" -OMtemplate \"{argument[3]}\" -output \"{[argument[4]}\" -s {argument[5]} -e {argument[6]}";

            }
            else if (j.RenderApp.ToLower() == "blender")
            {
                sendPacket.arguments[0] = "blender";
                //Blender Arguments breakdown: $"-b \"{args[1]}\" -s {args[5]} -e {args[6]} -a".
            }
            else if (j.RenderApp.ToLower() == "fusion")
            {
                //Not implemented yet.
                sendPacket.arguments[0] = "fusion";
            }
            
            c.renderTaskID = t.ID;
            c.JobID = j.ID;
            c.packetBuffer = sendPacket;
            c.Status = 1;
        }
        private static void checkForNewJobs(string path)
        {
            //Checks the CtlFolder for new job submissions
            //TODO: Change naming and add class based method for importing, rather than the local method.
            //TODO Someday: Rewrite AE and blender to somehow connect through tcpip. 

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
        }
        private static void ImportJob(string filePath)
        {
            //Imports job from importRender Type. 
            //TODO: Move method into importRender and remove this method. 

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
                if (split == 0) 
                {
                    //If a frame split is not specified, This will adjust split to try and create mostly equal chunk sizes between 10 and 50 frames.
                    //This should allow the shot to be rendered over a good number of computers but no so many that it causes problems.

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
                if (Import.Name == "") newJob.Name = "Unsaved Project";
                else newJob.Name = Import.Name;
                
                newJob.Project = Path.GetFullPath(HttpUtility.UrlDecode(Import.Project));
                newJob.WorkingProject = Import.WorkingProject;
                newJob.outputPath = Import.Filepath;
                if (Import.Filepath != "") newJob.outputDir = Directory.GetParent(Import.Filepath).ToString();
                else newJob.outputDir = "";
                newJob.RenderPreset = Import.outputType;
                newJob.FirstFrame = Convert.ToInt32(Math.Floor(Import.StartFrame));
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

            while (true)
            {
                //Checks if CtlFolder Archive contains a file with the same name, if true appends a random number string and checks again. 

                if (!File.Exists(ArchiveFilePath)) break;
                else
                {
                    ArchiveFilePath = Path.GetDirectoryName(filePath) + "\\Archive\\" + newJob.Name + "_" + RNG.Next(9999) + ".txt";
                }
            }
            if (inQueue.Contains(false)) return;
            else
            {
                try
                {
                    Thread.Sleep(RNG.Next(200));
                    File.Move(filePath, ArchiveFilePath);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.ReadLine();
                }
                
            }
            Database.UpdateDBFile = true;

        }
        private static List<renderTask> GenerateSteppedTasks(int start, int range, int split, DateTime _start, bool _vid, int Step, int _JID)
        {
            //Generates renderTasks from Job data.
            //TODO: Move into Job class and rewrite. 

            if (_vid) split = 1;
            Random rnd = new Random();
            List<renderTask> _return = new List<renderTask>();
            decimal AdjustedRange = decimal.Floor(range / Step);
            decimal roundSplitRange = decimal.Floor(AdjustedRange / split);

            int differance = Convert.ToInt32(AdjustedRange) - (Convert.ToInt32(roundSplitRange) * split);
            int firstFrame = start;
            int frameRange = 0;
            int AdjustedFrameCount = 0;
            for (int c = 0; c < split; c++)
            {
                renderTask nt = new renderTask();
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
                    firstFrame += (AdjustedFrameCount * Step);
                }
                else
                {
                    nt.finalFrame = firstFrame + frameRange - Step;
                    firstFrame += (AdjustedFrameCount * Step);
                }
                nt.GenerateFrameCount(Step);
                nt.ID = rnd.Next(1000000, 9999999);
                nt.JID = _JID;
                nt.finishTime = DateTime.MinValue;
                nt.Status = 0;
                nt.ProgressPerFrame = 100 / (float)nt.RenderFrameNumbers.Count();
                
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
                _return.Add(nt);
            }
            return _return;
        }
        private static void CheckActiveJobsProgress()
        {
            //Checks progress and estimates remaining time on active jobs.

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
                                    j.getProgress();
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