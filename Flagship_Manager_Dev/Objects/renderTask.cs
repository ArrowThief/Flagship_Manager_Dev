using Flagship_Manager_Dev.Components;
using Flagship_Manager_Dev.Objects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlagShip_Manager.Objects
{
    public class renderTask
    {
        //public List<string> Worker { get; set; } = new List<string> {""};
        //public int[]? WorkerIDs { get; set; } = new int[5];
        //public List<string> Log { get; set; } = new List<string> {""};
        //public string[] ArchiveLog { get; set; } = new string[1];
        //public List<string> managerLog { get; set; } = new List<string> {""};
        //public int[] LogLines { get; set; } = new int[5];
        
        public TaskLogs taskLogs { get; set; } = new TaskLogs();
        public string LastLogLine { get; set; } = "";
        public int ID { get; set; }
        public int JID { get; set; }
        public int Status { get; set; }//Job/Task status. queued(0), Rendering(1), finished(2) paused(3), fialed(4), canceled(5).
        public int adjustedFirstFrame { get; set; }
        public int adjustedFrameRange { get; set; }
        public int FirstFrame { get; set; }
        public int finalFrame { get; set; }
        //public int FinishedFrameCount { get; set; }
        //public int attempt { get; set; } = 0;
        public int[] RenderFrameNumbers { get; set; } = new int[0];
        public List<int> FinishedFrameNumbers { get; set; } = new List<int>();
        public float progress { get; set; } = 0;
        public float ProgressPerFrame { get; set; } = -1;
        public float ExistingProgress { get; set; } = 0;
        public int ExistingFrames { get; set; } = 0;
        public DateTime lastUpdate { get; set; }
        public DateTime finishTime { get; set; }
        public DateTime FinishReportedTime { get; set; }
        public DateTime LastFail { get; set; } = DateTime.MinValue;
        //public bool finished { get; set; } = false;
        public bool FinishReported { get; set; } = false;

        //public int OverLapGroup { get; set; } = 0;

        public void reset()
        {
            //if (now == DateTime.MinValue) now = DateTime.Now;
            Status = 0;
            //Worker = new List<string>();
            //WorkerIDs = new int[5];
            adjustedFirstFrame = FirstFrame;
            //finished = false;
            FinishReported = false;
            //attempt = 0;
            ExistingProgress = 0;
            ExistingFrames = 0;
            finishTime = DateTime.MinValue;
            //SubmitTime = new DateTime[5];
            lastUpdate = new DateTime();
            //Log.CopyTo(ArchiveLog);
            //Log.Clear();
            //managerLog.Clear();
            //SmallestFile = -1;
            taskLogs.ArhiveAndClear();
            progress = 0;
            //FinishedFrameCount = 0;
            FinishedFrameNumbers.Clear();
            //LogLines = new int[5];
            //managerLog = new List<string>();

        }
        public int Attempt(bool Index = true)
        {//Index shows index where by default it shows count. 
            int CurrentAttempt = taskLogs.Attempt(Index);
            return taskLogs.Attempt(Index);
            //else return taskLogs.Attempt() + 1; 
        }
        public WorkerObject? Worker()
        {
            WorkerObject? w = WorkerServer.WorkerList.Find(w => w.WorkerID == taskLogs.WorkerIDs.Last());
            return w;
        }
        public void Finish()
        {
            progress = 100;
            //FinishedFrameCount = adjustedFrameRange;
            FinishedFrameNumbers = RenderFrameNumbers.ToList();
            Status = 2;
            finishTime = DateTime.Now;
        }
        public void GetProgressFromAELog(float _fps = 0)
        {
            Regex LineRegex = new Regex(@"(PROGRESS:  )(\ *.+?\ )\(.+?\): [0-9]* Seconds");
            Regex Number = new Regex(@"(:  [\d]*)");
            Regex TimeCode = new Regex(@"([\d]*):([\d]*):([\d]*):([\d]*)"); 
            //double ProgressPercent = 100 / Convert.ToDouble(TotalFrames);
            double CurrentProgress = 0;
            //string FinalMatch = "0";
            string[] stringArray;
            if (taskLogs.WorkerLog.Count() >= Attempt(false) && Attempt(false) > 0)
            {
                stringArray = taskLogs.WorkerLog.Last().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            else return;
            if (_fps == 0)
            {
                //Finds FPS From Log adata. Can be finiky as AE is weird about log data. 
                findFPS(stringArray);
                return;
            }

            if (ProgressPerFrame == -1) ProgressPerFrame = 100 / (finalFrame - FirstFrame + 1);
            foreach (string line in stringArray)
            {
                if (LineRegex.IsMatch(line)) //Checks to see if log line is a frame progress line.
                {
                    if (stringArray.Any(s => TimeCode.IsMatch(s))) //Timecode mode
                    {
                        if (TimeCode.IsMatch(line))
                        {
                            var CurrentFrame = ConvertFromTimeCode(TimeCode.Match(line).Value, _fps);
                            if (RenderFrameNumbers.Any(f => f == CurrentFrame) && !FinishedFrameNumbers.Contains(CurrentFrame))
                            {
                                FinishedFrameNumbers.Add(CurrentFrame);
                            }
                        }
                    }
                    else
                    {
                        if (Number.IsMatch(line)) //Frame number mode.
                        {
                            var CurrentFrame = int.Parse(Number.Match(line).Value.Substring(3));
                            if (RenderFrameNumbers.Any(f => f == CurrentFrame) && !FinishedFrameNumbers.Contains(CurrentFrame))
                            {
                                FinishedFrameNumbers.Add(CurrentFrame);
                            }
                        }
                    }
                }
            }
            CurrentProgress += (ProgressPerFrame * FinishedFrameNumbers.Count());
            lastUpdate = DateTime.Now;
            
            FindAdjustedFirstFrame();
            
            CurrentProgress = Math.Round(CurrentProgress);
            progress = (float)CurrentProgress;


        }
        public void FindAdjustedFirstFrame()
        {
            for (int i = 0; i < RenderFrameNumbers.Count(); i++)
            {
                if (FinishedFrameNumbers.Count() == i)
                {
                    adjustedFirstFrame = RenderFrameNumbers[i];
                    return;
                }
                else if (RenderFrameNumbers[i] != FinishedFrameNumbers[i])
                {
                    adjustedFirstFrame = RenderFrameNumbers[i];
                    return;
                }

            }
            adjustedFirstFrame = 0;
        }
        internal void GenerateFrameCount(int Step)
        {
            List<int> Return = new List<int>();
            int count = FirstFrame;
            for (int i = 0; i < adjustedFrameRange; i++)
            {
                Return.Add(count);
                count += Step;
            }
            RenderFrameNumbers = Return.ToArray();
            //return Return.ToArray();
        }
        internal void taskFail(string ErrorLog, bool cancel = false, bool IgnoreAttempts = false) //Fail logger, Can add more types of failure and logs for them here.
        {
            //var EditableWorker = WorkerServer.WorkerList.Find(w => w ==_worker);
            try
            {
                Job? j = ParentJob();//jobManager.jobList.Find(j => j.ID == _jobID);
                WorkerObject w;
                //renderTask? t = j.renderTasks.Find(rT => rT.ID == _taskID);
                int WID = taskLogs.WorkerIDs.Last();
                bool missingWorker = false;
                try
                {
                    w = WorkerServer.WorkerList.Find((w) => w.WorkerID == taskLogs.WorkerIDs.Last());

                }
                catch
                {
                    missingWorker = true;
                    w = new WorkerObject();
                    w.name = "Missing Data";
                }

                Regex OutofVRAM = new Regex("(19969)");
                int LogIndex = Attempt();
                if (Status == 2) return;

                LastFail = DateTime.Now;
                ExistingProgress = progress;
                FinishReported = false;
                taskLogs.WriteToManager($"\n------Manager Fail Log Start------\n\n{ErrorLog}\n");

                if (cancel && !missingWorker)
                {
                    WorkerServer.cancelWorker(w, false, false);
                    taskLogs.WriteToManager($"\n{w.name} has been canceled.\n");
                    taskLogs.WriteToWorker($"\n{w.name} has been canceled.\n");
                }
                else
                {
                    j.ShotAlert = true;
                    if (OutofVRAM.IsMatch(taskLogs.WorkerLog.Last()))
                    {
                        taskLogs.WriteToManager($"\n{w.name} apears to have failed due to a lack of VRAM.");
                        if (!j.GPU && j.VRAMERROR > 2)
                        {
                            taskLogs.WriteToManager("\n\nMultiple VRAM ERRORS detected, GPU requirement has been enabled in an attempt stop this from happening again.");
                            j.GPU = true;
                        }
                        else j.VRAMERROR++;

                    }
                    else taskLogs.WriteToManager($"\n{w.name} has failed to render task before returning it to manager.");
                }
                //t.Log[t.attempt] += $"Log:\n {_worker.logData}\n{ErrorLog}";
                if (j.vid)
                {
                    j.Progress = 0;
                    progress = 0;
                    ExistingProgress = 0;
                    ExistingFrames = 0;
                    adjustedFirstFrame = FirstFrame;
                    FinishedFrameNumbers.Clear();
                }
                else
                {
                    foreach (int frame in RenderFrameNumbers)//Creates a list of unfinished fraems in Log;
                    {
                        if (!FinishedFrameNumbers.Contains(frame)) taskLogs.WriteToManager($"\nMissing Frame: {frame}");
                    }
                }
                if (LogIndex > 4)
                {
                    //j.Status = 4;
                    j.fail = true;
                    j.Status = 4;
                    Status = 4;
                    taskLogs.WriteToManager("Job failed to render more than 5 times, something is probably wrong with it.");
                    //w.ConsoleBuffer = j.failLog;
                    j.Status = 4;
                    taskLogs.WriteToManager($"\n------Manager Fail Log End------\n");
                }
                else
                {
                    taskLogs.WriteToManager("\n------Manager Fail Log End------\n");
                    Status = 0;
                    if (!missingWorker)
                    {
                        int workerFail = 0;
                        foreach (int ID in taskLogs.WorkerIDs)//Counts the number of times a worker has attempted this task.
                        {
                            if (ID == w.WorkerID) workerFail++;
                        }
                        if (workerFail > 2 && !IgnoreAttempts)
                        {
                            j.WorkerBlackList.Add(w.WorkerID);
                            w.lastSubmittion = DateTime.Now.AddSeconds(20);
                        }
                    }//t.attempt++;
                    //taskLogs.add();
                }

                return;
            }
            catch
            {
                return;
            }

        }
        internal void findFPS(string[] _stringArray)
        {
            Regex FindNum = new Regex("[+-]?([0-9]*[.])?[0-9]+");
            Regex FrameRate = new Regex("(PROGRESS:  Frame Rate: )"); //
            float fps = 0;

            foreach (string s in _stringArray)
            {
                if (FrameRate.IsMatch(s))
                {
                    fps = float.Parse(FindNum.Match(s).Value);
                    Job Parent = ParentJob();
                    Parent.frameRate = fps;
                    break;
                }
            }
            
        }
        internal Job ParentJob()
        {
            return jobManager.jobList.Find(j => j.ID == JID);
        }
        internal int ConvertFromTimeCode(string _TC, float _fps)
        {
            int LocalFPS = (int)Math.Round(_fps);

            string[] SplitString = _TC.Split(":");
            int Hours = int.Parse(SplitString[0]);
            int Minutes = int.Parse(SplitString[1]);
            int Seconds  = int.Parse(SplitString[2]);
            int Frames = int.Parse(SplitString[3]);

            float hourFrames = LocalFPS * 60*60;
            float minFrames = LocalFPS * 60;
            //float seconds = _fps;
            var TotalFrames = (Hours * hourFrames) + (Minutes * minFrames) + (Seconds * LocalFPS) + Frames;
            return (int)TotalFrames;

        }
        internal string ConvertToTimeCode(int _frame, float _fps)
        {
            int LocalFPS = (int)Math.Round(_fps);
            float TotalFrames = _frame;
            float hourFrames = LocalFPS * 60 * 60;
            float minFrames = LocalFPS * 60;
            //float seconds = _fps;

            int Hours = 0;
            int Minutes = 0;
            int seconds = 0;

            while (TotalFrames > hourFrames)
            {
                TotalFrames -= hourFrames;
                Hours++;
            }
            while (TotalFrames > minFrames)
            {
                TotalFrames -= minFrames;
                Minutes++;
            }
            while (TotalFrames > LocalFPS)
            {
                TotalFrames -= LocalFPS;
                seconds++;
            }
            return $"{Hours}:{Minutes}:{seconds}:{TotalFrames}";
        }
        public bool CheckWorkerActivity()
        {
            if (taskLogs.SubmitTime.Last().AddSeconds(30) < DateTime.Now)
            {
                WorkerObject w = WorkerServer.WorkerList.Find(w => w.WorkerID == taskLogs.WorkerIDs.Last());
                if (w != null)
                {
                    if (w.Status == 1)
                    {
                        if (w.renderTaskID == ID)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}
/*
 *         public void AELogtoProgress()
        {
            
            Regex LineRegex = new Regex(@"(PROGRESS:  )(\ *.+?\ )\(.+?\): [0-9]* Seconds");
            Regex Number = new Regex(@"\(.+?\)");
            int TotalFrames = (finalFrame - FirstFrame) + 1;
            //double ProgressPercent = 100 / Convert.ToDouble(TotalFrames);
            double CurrentProgress = 0;
            string FinalMatch = "0";
            string[] stringArray;
            if (taskLogs.WorkerLog.Count() >= Attempt(false) && Attempt(false) > 0)
            {
                stringArray = taskLogs.WorkerLog.Last().Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            else return; 
            
            int FrameInt = 0;
            if (ProgressPerFrame == -1) ProgressPerFrame = 100 / (float)TotalFrames;
            foreach (string line in stringArray)
            {
                if (LineRegex.IsMatch(line))
                {
                    if (Number.IsMatch(line))
                    {
                        FinalMatch = Number.Match(line).ToString();
                        Status = 1;
                    }
                }
            }
            if (FinalMatch != "0") FinalMatch = FinalMatch.Substring(1, (FinalMatch.Length - 2));

            try
            {
                FrameInt = int.Parse(FinalMatch);
            }
            catch
            {
                return;
            }
            CurrentProgress += (ProgressPerFrame * FrameInt);
            if (ExistingProgress != CurrentProgress) CurrentProgress = CurrentProgress + ExistingProgress;
            //FinishedFrameCount = FrameInt;
            //lastUpdate = DateTime.Now;

            if (FrameInt < TotalFrames) adjustedFirstFrame = FirstFrame + FrameInt;
            else adjustedFirstFrame = 0;
            CurrentProgress = Math.Round(CurrentProgress);
            progress = (float)CurrentProgress;


        }
 * internal void CheckTaskFinish(Job ParentJob)
		{
            if (Status == 2)
            {
                if (jobManager.CheckFiles(ParentJob, this))//Checks to make sure the files actually exist.
                {
                    progress = 100;
                    FinishedFrameCount = adjustedFrameRange;
                    FinishedFrameNumbers = RenderFrameNumbers.ToList();
                    Status = 2;
                    //finished = true;
                    finishTime = DateTime.Now;
                    ParentJob.UpdateUI = true;
                }
            }   
        }

  public void WriteManagerLog(string LogToWrite)
        //{
        //    if (managerLog.Count() < Log.Count()) managerLog.Add(LogToWrite);
        //    else managerLog[Log.Count() - 1] += LogToWrite;
        //}
        //public void IncreeseAttempt()
        //{
        //    Log.Add("");
        //    managerLog.Add("");
        //    Worker.Add("");

        //}
  internal void CheckForFail(int _MaxRenderTime, int _JID)
        {
            if (progress >= 100 || Status == 2) return;
            WorkerObject worker = new WorkerObject();

            try
            {

                worker = WorkerServer.WorkerList.Find(w => w.WorkerID == taskLogs.WorkerIDs.Last());
            }
            catch
            {
                Console.WriteLine("Couldn't idetify worker.");
                //Logic.taskFail(_JID, ID, 0000, $"Worker Could not be identified. Task reclaimed."); //Couldn't Identify worker.
                taskFail(_JID, $"Worker Could not be identified. Task reclaimed.");
                return;
            }
            if (progress >= 100 && worker.renderTaskID == ID)
            {
                WorkerServer.cancelWorker(worker, false, false);
                Status = 2;
                return;
            }
            if (FinishReportedTime.AddSeconds(20) > DateTime.Now) return;
            else if (FinishReported && progress < 100)
            {
                taskFail(_JID, $"{worker.name} reported it finished, but manager was unable to find finished frames.");
                //Logic.taskFail(_JID, ID, worker.WorkerID, $"{worker.name} reported it finished, but manager was unable to find finished frames.");
                FinishReported = false;
                return;
            }
            if (taskLogs.SubmitTime[Attempt()].AddMinutes(_MaxRenderTime) < DateTime.Now && progress == 0)//Kills worker if after a given time no progress has been made.
            {
                WorkerServer.cancelWorker(worker, false, false);
                //Logic.taskFail(_JID, ID, worker.WorkerID, $"Task Timed out after {_MaxRenderTime} minutes of no progress updates."); //Timeout Fail
                taskFail(_JID, $"Task Timed out after {_MaxRenderTime} minutes of no progress updates.");
                worker.renderTaskID = 0;
                worker.JobID = 0;
                worker.ConsoleBuffer = $"{worker.name} failed Task.";
            }
            else if (taskLogs.WorkerLog.Last() == "" && taskLogs.SubmitTime.Last().AddMinutes(5) < DateTime.Now)
            {
                try
                {
                    WorkerServer.cancelWorker(worker, false, false);
                    //Logic.taskFail(_JID, ID, worker.WorkerID, "Task Timed out after 5 minutes without getting startup log. Assumed worker crashed."); //Timeout Fail
                    taskFail(_JID, "Task Timed out after 5 minutes without getting startup log. Assumed worker crashed.");
                    worker.renderTaskID = 0;
                    worker.JobID = 0;
                    worker.ConsoleBuffer = $"Task Timed out after 5 minutes without getting startup log. Assumed worker crashed.\"";
                }
                catch (Exception Ex)
                {
                    Status = 0;
                    Console.WriteLine("ERROR: " + Ex);
                }
            }
            if (worker.Status == 7)
            {
                //var JTindex = Logic.matchWorkertoJob(worker);
                Readout.ReadoutBuffer = $"{worker.name} has dissconnected unexpectedly and its task will be reclaimed.";
                //Logic.taskFail(_JID, ID, worker.WorkerID, "has dissconnected unexpectedly during a render. Its assigned task will be reclaimed.");
                taskFail(_JID, "has dissconnected unexpectedly during a render. Its assigned task will be reclaimed.");)
            }
            if (worker.renderTaskID != ID && worker.Status == 1)
            {
                //Logic.taskFail(_JID, ID, 0000, $"Worker has been assigned other work before finishing this task."); //Couldn't Identify worker.
                taskFail(_JID, $"Worker has been assigned other work before finishing this task.");
                return;
            }
        }
 */