using FlagShip_Manager.Objects;
using System.Text.RegularExpressions;

namespace FlagShip_Manager
{
    internal class Logic
    {
        public static List<Job> getQueuedJobs(List<Job> jobList)
        {
            //Job/Task status. queued(0), Rendering(1), complete(2), paused(3), fialed(4), canceled(5).

            List<Job> _returnList = new List<Job>();
            for (int i = 0; i < jobManager.ActiveIDList.Count(); i++)
            {
                int JID = jobManager.ActiveIDList[i];
                Job j = jobList.Find(jo => jo.ID == JID);
                if (j.finished) continue;
                else if (j.fail)
                {
                    if (j.Status != 4)
                    {
                        j.Status = 4;

                        foreach (var rt in j.renderTasks)
                        {
                            if (rt.Status == 2 || rt.Status == 4) continue;
                            else if (rt.Status == 0 || rt.Status == 1) rt.Status = 5;
                            else rt.Status = 4;
                        }
                    }
                }
                switch (j.Status)
                {
                    case (0):
                        _returnList.Add(j);
                        break;
                    case (1):
                        _returnList.Add(j);
                        break;
                    default:
                        break;
                }
            }
            if (_returnList.Count > 0) _returnList.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return _returnList;
        }
        internal static List<int> getAvailableWorkers(Job _job)
        {//Client status. ready for work(0), rendering(1), Task complete(2), task pending(3), failed(4), canceled(5) starting up(6), offline(7).
            List<int> _return = new List<int>();
            var workers = WorkerServer.WorkerList;
            var activeJobs = new List<Job>();
            for(int jindex = 0; jindex < jobManager.jobList.Count(); jindex++)
            {
                Job j = jobManager.jobList[jindex];
                if (!j.Archive && (j.Status == 0 || j.Status == 1)) activeJobs.Add(j);//Builds a list of jobs that are currently waiting to be rendered.            
            }
            for (int wI = 0; wI < workers.Count(); wI++)
            {
                WorkerObject worker = workers[wI];
                RenderApp Default = worker.AvailableApps.Find(a => a.Default == true); //Check if the worker has a default render type.
                RenderApp CurrentJob = worker.AvailableApps.Find(a => a.AppName == _job.RenderApp);
                if (CurrentJob == null || worker.Dummy) continue;//Check if Worker can render this Job type.
                else if (!CurrentJob.Enabled) continue;
                //if (worker.Dummy) continue;
                if (worker.awaitUpdate || worker.lastSubmittion.AddSeconds(5) > DateTime.Now) continue;
                if (_job.WorkerBlackList.Contains(worker.WorkerID)) continue;//Checks the jobs blacklist for worker.
                if (_job.GPU && !worker.GPU) continue;

                if (worker.Status == 0)//Client is ready to work.
                {
                    if (Default != null)
                    {
                        if (Default.AppName != _job.RenderApp && Default.Enabled)//Checks if the worker's default render app is the same as the Jobs.
                        {
                            if (activeJobs.Any(j => j.RenderApp == Default.AppName)) continue; //If the worker is able to render this type but it isn't the default, checks if there is a default Job in the queue.   
                        }
                    }
                    _return.Add(workers.FindIndex(w => w == worker));//If one or more of the previous checks pass then worker is added to list of available workers for the job.
                }

            }

            return _return;
        }
        public static string GetRenderType(string _outputType, string _outputPath)
        {
            var Type = (_outputType);
            var FileType = Path.GetExtension(_outputPath).Substring(1);
            if (FileType.ToLower() == "mov")
            {
                if (Regex.IsMatch(Type, "(4444)"))
                {
                    return "4444";
                }
                else if (Regex.IsMatch(Type, "(422)"))
                {
                    return "422";
                }
            }
            if (Type == "Bob") Console.Write("Bob was here.");
            return FileType.ToUpper();
        }
        public static void BuildFolderPath(string dir) { 
        if (!Directory.Exists(dir))
            {
                BuildFolderPath(Directory.GetParent(dir).FullName);
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Failed to make folder at: " + dir);
                }
                
            }
        }
    }
}
/*
 * internal static int[] matchWorkertoJob(WorkerObject c)
        {
            bool found = false;
            int[] _return = new int[2];
            foreach (Job _job in jobManager.jobList)
            {
                if (_job.ID == c.JobID)
                {
                    found = true;
                    _return[0] = jobManager.jobList.FindIndex(j => j == _job);
                    _return[1] = _job.renderTasks.FindIndex(rt => rt.ID == c.renderTaskID);
                }
            }
            if (!found) _return[0] = -1;
            return _return;
        }
 * public static void estimateRemainingTime(Job j)//This will calculate the estimated remaining time.
        {
            double remainingProgress = 100 - j.Progress;
            TimeSpan TotalTime = TimeSpan.Zero;
            TimeSpan MachineHours = TimeSpan.Zero;
            int count = 0;
            int LogIndex = 0;
            DateTime Earliest = DateTime.Now;
            DateTime EndTime = DateTime.MinValue;//j.renderTasks[0].lastUpdate;
            if (j.Status == 1)
            {
                foreach (renderTask rt in j.renderTasks)
                {
                    if (rt.Status == 1 || rt.Status == 2)
                    {
                        LogIndex = rt.Attempt();
                        if (rt.finishTime > DateTime.MinValue)
                        {
                            MachineHours += rt.finishTime - rt.taskLogs.SubmitTime[LogIndex];
                        }
                        else
                        {
                            MachineHours += DateTime.Now - rt.taskLogs.SubmitTime[LogIndex];
                        }
                    }
                }
                j.MachineHours = MachineHours;
                if (j.StartTimes.Count() < 1) return;
                foreach (DateTime StartT in j.StartTimes)
                {
                    if (count >= j.EndTimes.Count()) EndTime = DateTime.Now;
                    else EndTime = j.EndTimes[count];
                    if (EndTime - StartT < TimeSpan.FromSeconds(5))
                    {
                        EndTime = DateTime.Now;
                    }
                    TotalTime += EndTime - StartT;
                    count++;
                }
            }
            else return;
            //timeDifferance = Latest - Earliest;
            double ppsBuffer = TotalTime.TotalSeconds / j.Progress;
            j.TimePerFrame = Math.Round(TotalTime.TotalSeconds / j.CompletedFrames, 3);
            if (double.IsNaN(ppsBuffer) || double.IsInfinity(ppsBuffer) || ppsBuffer < .25) return;
            else
            {
                if (j.ProgressPerSecond.Count() < 30) j.ProgressPerSecond.Add(ppsBuffer);
                else
                {
                    j.ProgressPerSecond.RemoveAt(0);
                    j.ProgressPerSecond.Add(ppsBuffer);
                }
            }
            double averagePPS = Enumerable.Sum(j.ProgressPerSecond) / 30;
            j.totalActiveTime = TotalTime;
            try//This will probably need to be fixed, but it keeps thing from crashing.
            {
                j.RemainingTime = TimeSpan.FromSeconds(Math.Round(remainingProgress * averagePPS));
            }
            catch
            {
                j.RemainingTime = TimeSpan.Zero;
            }
        }
 *         internal static int matchTasktoWorker(renderTask rt)
        {
            var WL = WorkerServer.WorkerList;
            var WID = rt.taskLogs.WorkerIDs.Last();
            return WL.FindIndex((w) => w.WorkerID == WID);
        }
   internal static void taskFail(int _jobID, int _taskID, int wID, string ErrorLog, bool cancel = false, bool IgnoreAttempts = false) //Fail logger, Can add more types of failure and logs for them here.
        {
            //var EditableWorker = WorkerServer.WorkerList.Find(w => w ==_worker);
            try
            {
                Job? j = jobManager.jobList.Find(j => j.ID == _jobID);
                renderTask? t = j.renderTasks.Find(rT => rT.ID == _taskID);
                WorkerObject w = WorkerServer.WorkerList[matchTasktoWorker(t)];

                Regex OutofVRAM = new Regex("(19969)");
                int LogIndex = t.Attempt();
                if (t.Status == 2) return;

                t.LastFail = DateTime.Now;
                t.ExistingProgress = t.progress;
                t.FinishReported = false;
                t.taskLogs.WriteToManager($"\n------Manager Fail Log Start------\n\n{ErrorLog}\n");

                if (cancel)
                {
                    WorkerServer.cancelWorker(w, false, false);
                    t.taskLogs.WriteToManager($"\n{w.name} has been canceled.\n");
                    t.taskLogs.WriteToWorker($"\n{w.name} has been canceled.\n");
                }
                else
                {
                    j.ShotAlert = true;
                    if (OutofVRAM.IsMatch(t.taskLogs.WorkerLog.Last()))
                    {
                        t.taskLogs.WriteToManager($"\n{w.name} apears to have failed due to a lack of VRAM.");
                        if (!j.GPU && j.VRAMERROR > 3)
                        {
                            t.taskLogs.WriteToManager("\n\nMultiple VRAM ERRORS detected, GPU requirement has been enabled in an attempt stop this from happening again.");
                            j.GPU = true;
                        }
                        else j.VRAMERROR++;

                    }
                    else t.taskLogs.WriteToManager($"\n{w.name} has failed to render task before returning it to manager.");
                }
                //t.Log[t.attempt] += $"Log:\n {_worker.logData}\n{ErrorLog}";
                if (j.vid)
                {
                    j.Progress = 0;
                    t.progress = 0;
                    t.ExistingProgress = 0;
                    t.ExistingFrames = 0;
                    t.adjustedFirstFrame = t.FirstFrame;
                }
                else
                {
                    foreach (int frame in t.RenderFrameNumbers)//Creates a list of unfinished fraems in Log;
                    {
                        if (!t.FinishedFrameNumbers.Contains(frame)) t.taskLogs.WriteToManager($"\nMissing Frame: {frame}");
                    }
                }
                if (LogIndex > 4)
                {
                    //j.Status = 4;
                    j.fail = true;
                    j.Status = 4;
                    t.Status = 4;
                    t.taskLogs.WriteToManager("Job failed to render more than 5 times, something is probably wrong with it.");
                    //w.ConsoleBuffer = j.failLog;
                    j.Status = 4;
                    t.taskLogs.WriteToManager($"\n------Manager Fail Log End------\n");
                }
                else
                {
                    t.taskLogs.WriteToManager("\n------Manager Fail Log End------\n");
                    t.Status = 0;
                    int workerFail = 0;
                    foreach (int ID in t.taskLogs.WorkerIDs)//Counts the number of times a worker has attempted this task.
                    {
                        if (ID == w.WorkerID) workerFail++;
                    }
                    if (workerFail > 2 && !IgnoreAttempts)
                    {
                        j.WorkerBlackList.Add(w.WorkerID);
                        w.lastSubmittion = DateTime.Now.AddSeconds(20);
                    }
                    //t.attempt++;
                    //t.taskLogs.add();
                }

                return;
            }
            catch
            {
                return;
            }

        }
 */
