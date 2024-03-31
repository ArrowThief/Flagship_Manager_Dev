using FlagShip_Manager.Management_Server;
using Flagship_Manager_Dev.Objects;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FlagShip_Manager.Objects
{
    public class Job
    {
        //A container for all of a Jobs required data, includes a list of renderTasks to be sent to workers. 
        //TODO: Split UI info out of job for faster UI interface. 

        //Render info
        public string Project { get; set; } = "null";
        public string WorkingProject { get; set; } = "Data Not found";
        public string Name { get; set; } = "null";
        public string RenderPreset { get; set; } = "null";
        public string outputPath { get; set; } = "null";
        public string outputDir { get; set; } = "null";
        public string RenderApp { get; set; } = "null";
        public int FirstFrame { get; set; }
        public int FrameRange { get; set; }
        public int FrameStep { get; set; } = 1;
        public int TotalFramesToRender { get; set; }
        public int QueueIndex { get; set; } = -1;

        //Progress info
        public bool Archive { get; set; } = false;
        public bool ShotAlert { get; set; } = false;
        public int Priority { get; set; }
        public int CompletedFrames { get; set; }
        public float frameRate { get; set; } = 0;
        public float Progress { get; set; }
        public float ProgressPerFrame { get; set; } = -1;
        public List<double>? ProgressPerSecond { get; set; }
        public TimeSpan RemainingTime { get; set; }
        public TimeSpan totalActiveTime { get; set; }
        public TimeSpan MachineHours { get; set; } = TimeSpan.Zero;
        public TimeSpan OutputDeviceOffset { get; set; }


        //Metadata for render logic.
        public string FileFormat { get; set; } = "";
        public int ID { get; set; } = -1;
        public bool PauseRequest { get; set; } = false;
        public int Status { get; set; } //Job/Task status. queued(0), Rendering(1), finished(2) paused(3), fialed(4), canceled(5).
        public bool fail { get; set; }
        public bool GPU { get; set; }
        public int VRAMERROR { get; set; } = 0;
        public bool Overwrite { get; set; }
        public bool finished { get; set; }
        public bool vid { get; set; }
        public double TimePerFrame { get; set; } = 0;
        public DateTime CreationTime { get; set; }
        public DateTime ArchiveDate { get; set; } = DateTime.MaxValue;
        public List<int> WorkerBlackList { get; set; } = new List<int>();
        public List<renderTask> renderTasks { get; set; } = new List<renderTask>();
        public List<DateTime> StartTimes { get; set; } = new List<DateTime>();
        public List<DateTime> EndTimes { get; set; } = new List<DateTime>();
        public bool UpdateUI { get; set; } = false;
        public bool selected { get; set; } = false;
        public double SmallestFile { get; set; } = -1;

        public void getProgress()
        {
            //Updates Job progress info. 

            float localProgress = 0;
            int localFinishedFrames = 0;
            foreach (renderTask task in renderTasks)
            {
                if (RenderApp.ToLower() == "ae")
                {
                    //AfterEffects Render.

                    if (task.Status == 1)
                    {
                        //Task is being rendered currently

                        task.GetProgressFromAELog(frameRate);
                        if (task.CheckWorkerActivity())
                        {
                            task.FinishReported = true;
                        }
                    }
                    if (task.FinishReported)
                    {
                        task.FinishReported = false;
                        if (CheckFiles(task, false, true))
                        {
                            //Checks all the files after a task is returned to Manager, will overwrite progress data from AE Log.

                            task.Finish();
                            UpdateUI = true;
                        }
                        else
                        {
                            //Some or all frames were missing dispite Log data.

                            task.taskFail("The worker returned task to Manager without completing it.");
                        }
                    }

                }
                else
                {
                    //Blender render.

                    if (task.Status == 1)
                    {
                        if (CheckFiles(task))
                        {
                            //Gets progress frome file existance, age and readabiliity.

                            task.Finish();
                            UpdateUI = true;
                        }
                        else if (task.FinishReported)
                        {
                            task.FinishReported = false;
                            if (task.finishTime.AddMinutes(1) < DateTime.Now && task.progress < 100)
                            {
                                task.taskFail("The worker returned task to Manager, yet some or all frames appear to be absent.");
                            }
                        }
                    }
                }
                //Adds progress from each tasks finished frames to local variable.
                localProgress += task.FinishedFrameNumbers.Count() * ProgressPerFrame;

                localFinishedFrames += task.FinishedFrameNumbers.Count();
            }

            if (localProgress > Progress)
            {
                //If all tasks progress is grater than previous Progress report then Progress is updated. 

                Progress = (float)Math.Round(localProgress);
                CompletedFrames = localFinishedFrames;
            }

            if (Progress >= 100 || renderTasks.All(t => t.Status == 2))
            {
                //Job finished

                JobsDone();
                return;
            }

            if (PauseRequest)
            {
                //Pause

                Status = 3;
            }
            else if (renderTasks.Any(t => t.Status == 1))
            {
                //Tasks are rendering

                Status = 1;
            }
            else if (Status != 2 && Status != 3 && Status != 5)
            {
                //No Tasks are rendering

                if (StartTimes.Count() > EndTimes.Count() && DateTime.Now - StartTimes.Last() > TimeSpan.FromSeconds(5))
                {
                    EndTimes.Add(DateTime.Now);
                }

                if (Status != 4) Status = 0;
                Thread.Sleep(500);
            }
        }
        public void JobsDone()
        {
            //Marks Job and all renderTasks to finished.
            //Catches weird issues that happen sometimes.

            foreach (renderTask rt in renderTasks)
            {
                if (rt.Status != 2 || rt.progress < 100 || rt.finishTime == DateTime.MinValue)
                {
                    rt.Status = 2;
                    rt.progress = 100;
                    rt.finishTime = DateTime.Now;
                }
            }
            Status = 2;
            finished = true;
            Progress = 100;
            EndTimes.Add(DateTime.Now);
            RemainingTime = TimeSpan.Zero;
            CompletedFrames = TotalFramesToRender;
        }
        public void TimeEstimate()
        {
            //Calculates the reamaining time.

            double remainingProgress = 100 - Progress;
            TimeSpan TotalTime = TimeSpan.Zero;
            TimeSpan localMHours = TimeSpan.Zero;
            int count = 0;
            DateTime Earliest = DateTime.Now;
            DateTime EndTime = DateTime.MinValue;

            foreach (renderTask rt in renderTasks)
            {
                if (rt.Status == 1 || rt.Status == 2)
                {
                    if (rt.finishTime > DateTime.MinValue)
                    {
                        localMHours += rt.finishTime - rt.taskLogs.SubmitTime[rt.Attempt()];
                    }
                    else
                    {
                        localMHours += DateTime.Now - rt.taskLogs.SubmitTime[rt.Attempt()];
                    }
                }
            }
            MachineHours = localMHours;
            if (StartTimes.Count() < 1) return;
            foreach (DateTime StartT in StartTimes)
            {
                if (count >= EndTimes.Count()) EndTime = DateTime.Now;
                else EndTime = EndTimes[count];
                if (EndTime - StartT < TimeSpan.FromSeconds(5))
                {
                    EndTime = DateTime.Now;
                }
                TotalTime += EndTime - StartT;
                count++;
            }

            double ppsBuffer = TotalTime.TotalSeconds / Progress;
            TimePerFrame = Math.Round(TotalTime.TotalSeconds / CompletedFrames, 3);
            if (double.IsNaN(ppsBuffer) || double.IsInfinity(ppsBuffer) || ppsBuffer < .25) return;
            else
            {
                if (ProgressPerSecond.Count() < 30) ProgressPerSecond.Add(ppsBuffer);
                else
                {
                    ProgressPerSecond.RemoveAt(0);
                    ProgressPerSecond.Add(ppsBuffer);
                }
            }
            double averagePPS = Enumerable.Sum(ProgressPerSecond) / 30;
            totalActiveTime = TotalTime;
            try
            {
                //TODO: Check if this causes any problems.

                RemainingTime = TimeSpan.FromSeconds(Math.Round(remainingProgress * averagePPS));
            }
            catch
            {
                RemainingTime = TimeSpan.Zero;
            }
        }
        public void SetOutputOffset()
        {
            //Gets the time offset on the destination server.

            Random rng = new Random();

            string TimeOffsetPath = $"{outputDir}\\TimeTest_{rng.Next()}.txt";
            DateTime creationTime;
            try
            {
                File.WriteAllText(TimeOffsetPath, outputDir);
                creationTime = File.GetLastWriteTime(TimeOffsetPath);
                DateTime LocalTime = DateTime.Now;
                TimeSpan offset = creationTime - LocalTime;
                OutputDeviceOffset = offset;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to write time test file\n\n" + e);
            }

            Thread.Sleep(100);
            File.Delete(TimeOffsetPath);
        }
        public bool CheckFiles(renderTask _rT, bool DBLoad = false, bool checkAllFrames = false)
        {
            //Checks completion using file data. Also updates progress. Returns true if all files are found and openable. 
            //

            if (checkAllFrames) _rT.FinishedFrameNumbers.Clear();
            Regex numPattern = new Regex(@"(\[#+\])");

            double size = 0;
            DateTime AdjustedAge;
            DateTime fileAge = new DateTime();

            int LogIndex = _rT.Attempt();

            string extention = Path.GetExtension(outputPath).ToLower();
            string fileName = Path.GetFileNameWithoutExtension(outputPath).ToLower();
            string filePath = $@"{outputDir}\";
            string Padding = "";


            if (vid)
            {
                if (!File.Exists(outputPath)) return false;
                var FI = new FileInfo(outputPath);
                fileAge = FI.LastWriteTime;
                AdjustedAge = fileAge - OutputDeviceOffset;
                size = FI.Length;
                if (FileCheck.CheckFileReadability(outputPath) && fileAge > CreationTime)
                {
                    return true;
                }
                else return false;

            }

            bool appendtoEnd = false;
            int paddingCount = 0;
            int Step = FrameStep;
            string match = "";
            Regex NumReplacePatern = new Regex("");
            if (RenderApp.ToLower() == "ae")
            {
                NumReplacePatern = new Regex(@"(\[#+\])");
                paddingCount = Regex.Match(fileName, @"#+(?=])").Length;
                match = NumReplacePatern.Matches(fileName).First().Value;
            }
            else if (RenderApp.ToLower() == "fusion")
            {
                NumReplacePatern = new Regex(@"[0-9]+$");
                paddingCount = NumReplacePatern.Match(fileName).Length;
                match = NumReplacePatern.Match(fileName).Value;
            }
            else if (RenderApp.ToLower() == "blender")
            {
                NumReplacePatern = new Regex(@"(#+)");
                paddingCount = NumReplacePatern.Match(fileName).Length;
                match = NumReplacePatern.Matches(fileName).Last().Value;
            }
            else return false;
            if (match == "") appendtoEnd = true;
            if (_rT.RenderFrameNumbers.Count() == 0) _rT.GenerateFrameCount(Step);
            List<int> UnfinishedFrames = new List<int>();
            foreach (int frame in _rT.RenderFrameNumbers)
            {
                //Creates a list of unfinished fraems;

                if (!_rT.FinishedFrameNumbers.Any(f => f == frame))
                {
                    UnfinishedFrames.Add(frame);
                }
            }
            bool done = true;
            foreach (var frameNum in UnfinishedFrames)
            {
                string filePathwithNum = "";
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

                    try
                    {
                        var FI = new FileInfo(filePathwithNum);
                        fileAge = FI.LastWriteTime;
                        AdjustedAge = fileAge - OutputDeviceOffset;
                        size = FI.Length;

                    }
                    catch
                    {
                        _rT.taskLogs.WriteToManager($"Failed to get file info on frame: {frameNum}");
                        done = false;
                        continue;
                    }
                    if ((Overwrite && _rT.taskLogs.SubmitTime[0] < AdjustedAge) || !Overwrite)
                    {
                        //File Found and passed age check

                        if (size >= SmallestFile && SmallestFile > 0)
                        {
                            //File is assumed good by comparing the smallest known good file to this file's size.  

                            _rT.FinishedFrameNumbers.Add(frameNum);
                            _rT.taskLogs.WriteToManager($"{frameNum} Found, assumed good by size. \n");
                        }
                        else if (FileCheck.CheckFileReadability(filePathwithNum))
                        {
                            //File failed size check, fallback to explicet test and passed.
                            //This is FAR more disk intensive and would slow everything down consiterably if done to each file individually.

                            _rT.taskLogs.WriteToManager($"{frameNum} Found, checked explicitly\n");

                            if (size > 1024)
                            {
                                //Update smallest file size.

                                SmallestFile = Convert.ToDouble(size);
                                _rT.taskLogs.WriteToManager($"Smallest file size set to: {Math.Round(size / 1024)}KB\n");
                            }

                            _rT.FinishedFrameNumbers.Add(frameNum);
                        }
                        else
                        {
                            //File exists but is assumed corrupt. 

                            _rT.taskLogs.WriteToManager($"{frameNum} Found, but failed integreity check.\n");
                            done = false;
                        }
                    }
                    else
                    {
                        //File with correct name was found, but it failed age check.

                        done = false;
                        _rT.taskLogs.WriteToManager($"{frameNum} Old file was found, ignoring.\n");
                    }
                }
                else
                {
                    //File not found

                    done = false;
                    _rT.taskLogs.WriteToManager($"{frameNum} Not found.\n");
                }
            }
            _rT.FinishedFrameNumbers.Sort();
            _rT.adjustedFirstFrame = 0;

            _rT.FindAdjustedFirstFrame();
            _rT.progress = (float)Math.Ceiling(_rT.FinishedFrameNumbers.Count() * _rT.ProgressPerFrame);

            if (done)
            {
                //Jobs done.
                return true;
            }
            else
            {
                //More work? 
                return false;
            }
        }

        internal void BuiildOutputDir()
        {
            //Checks if output dir exits. If false, attempts to build dir recursively.
            //TODO: move this Method into the one that references it. 

            if (!Directory.Exists(outputDir))
            {
                Console.WriteLine("Output folder doesn't exists. Building...");
                try
                {
                    Logic.BuildFolderPath(outputDir);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Attempting to build folder path for new Job.\nERROR: \n" + e);
                }

            }
            else Console.WriteLine("Output folder exists.");

        }
    }
}