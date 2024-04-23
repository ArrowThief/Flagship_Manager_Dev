using FlagShip_Manager.Management_Server;
using Flagship_Manager_Dev.Components;
using Flagship_Manager_Dev.Objects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlagShip_Manager.Objects
{
    public class renderTask
    {
        //Part of Job, split into smaller parts for multiple workers.
        //TODO: 

        public TaskLogs taskLogs { get; set; } = new TaskLogs();
        public string LastLogLine { get; set; } = "";
        public Job ParentJob { get; set; }

        //Job/Task status. queued(0), Rendering(1), finished(2) paused(3), fialed(4), canceled(5).
        public int Status { get; set; }
        
        public int adjustedFirstFrame { get; set; }
        public int adjustedFrameRange { get; set; }
        public int FirstFrame { get; set; }
        public int finalFrame { get; set; }
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
        public bool FinishReported { get; set; } = false;

        public void reset()
        {
            //Resets renderTask status.

            Status = 0;
            adjustedFirstFrame = FirstFrame;
            FinishReported = false;
            ExistingProgress = 0;
            ExistingFrames = 0;
            finishTime = DateTime.MinValue;
            lastUpdate = new DateTime();
            taskLogs.ArhiveAndClear();
            progress = 0;
            FinishedFrameNumbers.Clear();

        }
        public int Attempt(bool Index = true)
        {
            //Returns how many time renderTaks has been attempted. Can be reset using reset().

            int CurrentAttempt = taskLogs.Attempt(Index);
            return taskLogs.Attempt(Index);
        }
        public Worker? Worker()
        {
            //Returns worker object
            //TODO: Switch to Binary search to find workers.

            Worker? w = DB.FindWorker(DB.workers, taskLogs.WorkerIDs.Last());
            return w;
        }
        public void Finish()
        {
            //Marks renderTask finished.

            progress = 100;
            FinishedFrameNumbers = RenderFrameNumbers.ToList();
            Status = 2;
            finishTime = DateTime.Now;
        }
        
        public void GetProgressFromAELog(float _fps = 0)
        {
            //Gets progress from AfterEffects Log data.

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
                if (LineRegex.IsMatch(line)) 
                {
                    //Checks to see if log line is a frame progress line.

                    if (stringArray.Any(s => TimeCode.IsMatch(s))) 
                    {
                        //Timecode mode

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
                        //Frame number mode.

                        if (Number.IsMatch(line)) 
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
            //Sets begining of Render Index if renderTask needs to be started again.

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
            //Generates a list of frame numbers that need to be rendered.
            //Used for checking output files.
            //Updates object Directly.

            List<int> Return = new List<int>();
            int count = FirstFrame;
            for (int i = 0; i < adjustedFrameRange; i++)
            {
                Return.Add(count);
                count += Step;
            }
            RenderFrameNumbers = Return.ToArray();
        }
        internal void taskFail(string ErrorLog, bool cancel = false, bool IgnoreAttempts = false) 
        {
            //Attempts to report reason for fail. Marks task as failed or moves to next attempt index.

            try
            {
                Worker w;
                int WID = taskLogs.WorkerIDs.Last();
                bool missingWorker = false;
                try
                {
                    w = Worker();

                }
                catch
                {
                    missingWorker = true;
                    w = new Worker();
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
                    //renderTask was canceled. 

                    WorkerServer.cancelWorker(w, false, false);
                    taskLogs.WriteToManager($"\n{w.name} has been canceled.\n");
                    taskLogs.WriteToWorker($"\n{w.name} has been canceled.\n");
                }
                else
                {
                    //Report reason for fail. 

                    ParentJob.ShotAlert = true;
                    if (OutofVRAM.IsMatch(taskLogs.WorkerLog.Last()))
                    {
                        //Checks for AfterEffects VRAM error code

                        taskLogs.WriteToManager($"\n{w.name} apears to have failed due to a lack of VRAM.");
                        if (!ParentJob.GPU && ParentJob.VRAMERROR > 2)
                        {
                            //Too many VRAM errors. Switch GPU requirement on.
                            //Only Workers with >4GB of VRAM will be seleced.

                            taskLogs.WriteToManager("\n\nMultiple VRAM ERRORS detected, GPU requirement has been enabled in an attempt stop this from happening again.");
                            ParentJob.GPU = true;
                        }
                        else ParentJob.VRAMERROR++;

                    }
                    else
                    {
                        //Fail without known reason. 

                        taskLogs.WriteToManager($"\n{w.name} has failed to render task before returning it to manager.");
                    }
                }
                if (ParentJob.vid)
                {
                    //Output is a video file.

                    ParentJob.Progress = 0;
                    progress = 0;
                    ExistingProgress = 0;
                    ExistingFrames = 0;
                    adjustedFirstFrame = FirstFrame;
                    FinishedFrameNumbers.Clear();
                }
                else
                {
                    //Output is an image sequence. 

                    foreach (int frame in RenderFrameNumbers)
                    {
                        //Creates a list of unfinished frames in Log;
                        //TODO: Rewrite for effecincy.

                        if (!FinishedFrameNumbers.Contains(frame)) taskLogs.WriteToManager($"\nMissing Frame: {frame}");
                    }
                }
                if (LogIndex > 4)
                {
                    //Too many renderTask fails. Full Job fail.

                    ParentJob.fail = true;
                    ParentJob.Status = 4;
                    ParentJob.Status = 4;
                    Status = 4;
                    
                    taskLogs.WriteToManager("Job failed to render more than 5 times, something is probably wrong with it.");
                    taskLogs.WriteToManager($"\n------Manager Fail Log End------\n");
                }
                else
                {
                    //renderTask fail only.

                    taskLogs.WriteToManager("\n------Manager Fail Log End------\n");
                    Status = 0;
                    if (!missingWorker)
                    {
                        int workerFail = 0;
                        foreach (int ID in taskLogs.WorkerIDs)
                        {
                            //Counts the number of times a worker has attempted this task.

                            if (ID == w.ID) workerFail++;
                        }
                        if (workerFail > 2 && !IgnoreAttempts)
                        {
                            //Black list workers with >3 fails.

                            ParentJob.WorkerBlackList.Add(w.ID);
                            w.lastSubmittion = DateTime.Now.AddSeconds(20);
                        }
                    }
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
            //Sets AfterEffects Job frame rate.

            Regex FindNum = new Regex("[+-]?([0-9]*[.])?[0-9]+");
            Regex FrameRate = new Regex("(PROGRESS:  Frame Rate: )"); //
            float fps = 0;

            foreach (string s in _stringArray)
            {
                if (FrameRate.IsMatch(s))
                {
                    fps = float.Parse(FindNum.Match(s).Value);
                    ParentJob.frameRate = fps;
                    break;
                }
            }
            
        }

        internal int ConvertFromTimeCode(string _TC, float _fps)
        {
            //Converts from timecode to frame number.

            string[] SplitString = _TC.Split(":");
            int LocalFPS = (int)Math.Round(_fps);
            int Hours = int.Parse(SplitString[0]);
            int Minutes = int.Parse(SplitString[1]);
            int Seconds  = int.Parse(SplitString[2]);
            int Frames = int.Parse(SplitString[3]);

            float hourFrames = LocalFPS * 60*60;
            float minFrames = LocalFPS * 60;

            var TotalFrames = (Hours * hourFrames) + (Minutes * minFrames) + (Seconds * LocalFPS) + Frames;
            return (int)TotalFrames;

        }
        internal string ConvertToTimeCode(int _frame, float _fps)
        {
            //Converts from frame number to timecode

            int LocalFPS = (int)Math.Round(_fps);
            int Hours = 0;
            int Minutes = 0;
            int seconds = 0;

            float TotalFrames = _frame;
            float hourFrames = LocalFPS * 60 * 60;
            float minFrames = LocalFPS * 60;

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
            //Checks if worker is active.

            if (taskLogs.SubmitTime.Last().AddSeconds(30) < DateTime.Now)
            {
                Worker w = WorkerServer.Find(taskLogs.WorkerIDs.Last());
                if (w != null && w.Status == 1)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}