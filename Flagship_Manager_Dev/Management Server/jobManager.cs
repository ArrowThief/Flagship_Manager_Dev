using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using Flagship_Manager_Dev;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace FlagShip_Manager
{
    internal class jobManager
    {
        //Stores and imports Jobs, manages RenderTasks distrobution, also monitors job progress and estimates time.
        //TODO: Breakup into new objects. 

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
            Thread dbc = new Thread(() => DB.DataBaseManager());
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

                if (DB.active.Count() > 0 && !DB.Startup)
                {
                    activeJobs = Logic.getQueuedJobs(DB.active);
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
                            for (int ri = 0; ri < _job.renderTasks.Length; ri++)
                            {
                                renderTask rT = _job.renderTasks[ri];
                                if (rT.Status == 0)
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

                                    if (rT.Status == 2)
                                    {
                                        continue;
                                    }
                                    Worker worker = DB.WorkerList[wi];
                                    if (worker.AvailableApps.Any(a => a.AppName == _job.RenderApp))
                                    {
                                        try
                                        {
                                            worker.lastSubmittion = DateTime.Now;
                                            if (_job.Status == 0)
                                            {
                                                _job.StartTimes.Add(DateTime.Now);
                                            }
                                            rT.taskLogs.add();
                                            worker.sendTasktoClientBuffer(_job, rT, ri);
                                            rT.taskLogs.WriteToWorker($" Task {TaskIndex} submitted to {worker.name} for rendering.\n------------------------------Worker Log start------------------------------\n", false);
                                            _job.Status = 1;
                                            rT.Status = 1;
                                            rT.taskLogs.SubmitTime[rT.Attempt()] = DateTime.Now;
                                            rT.taskLogs.WriteID(worker.WorkerID);
                                            if (clearAvailableWorkers) break;
                                            worker.ConsoleBuffer = ($"Sending: {_job.Name} task index: {ri} of {_job.renderTasks.Count()} to {DB.WorkerList[wi].name}");
                                            worker.awaitUpdate = true;
                                            break;
                                        }
                                        catch
                                        {
                                            worker.renderTaskIndex = -1;
                                            worker.JobID = 0;
                                            worker.ConsoleBuffer = $"Failed to send task to {worker.name} buffer";
                                            rT.Status = 0;
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
                                    //creates Job object from Job file.

                                    IList<importRender> imports = new List<importRender>();
                                    using (var Stream = new StreamReader(_job))
                                    {
                                        imports = JsonSerializer.Deserialize<List<importRender>>(Stream.ReadToEnd());
                                    }
                                    foreach (importRender j in imports)
                                    {
                                        Job newJ = j.JsonToJob();
                                        if (newJ.renderTasks.Length > 0)
                                        {
                                            DB.active.Add(newJ);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Failed to import Job.");
                                        }
                                        
                                        while (true)
                                        {
                                            //Checks if CtlFolder contains an Archive folder.
                                            //Then checks if the Archive folder contains a file with the same name, if true appends a random number string and checks again. 

                                            string ArchiveFilePath = Path.GetDirectoryName(_job) + "\\Archive\\" + j.Name + ".txt";
                                            if (!File.Exists(ArchiveFilePath)) break;
                                            else
                                            {
                                                ArchiveFilePath = Path.GetDirectoryName(_job) + "\\Archive\\" + j.Name + "_" + RandomNumberGenerator.GetInt32(9999) + ".txt";
                                            }

                                            try
                                            {
                                                File.Move(_job, ArchiveFilePath);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.ToString());
                                            }

                                            DB.UpdateDBFile = true;
                                        }
                                    }
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
        
        
        private static void CheckActiveJobsProgress()
        {
            //Checks progress and estimates remaining time on active jobs.

            List<string> ProgressThreads = new List<string>();
            while (true)
            {
                for (int ji = 0; ji < DB.active.Count(); ji++)
                {
                    Job j = DB.active[ji];
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