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

        public void newProgress()
        {
            float localProgress = 0;
            int localFinishedFrames = 0;
            foreach (renderTask task in renderTasks)
            {
                if (RenderApp.ToLower() == "ae")
                {
                    if (task.Status == 1)
                    {
                        task.GetProgressFromAELog(frameRate);//Gets progress from AE Log. 
                        if (task.CheckWorkerActivity())
                        {
                            task.FinishReported = true;
                        }
                    }
                    if (task.FinishReported)
                    {
                        task.FinishReported = false;
                        if (CheckFiles(task, false, true))//Checks all the files after a task is returned to Manager, will overwrite progress data from AE Log.
                        {
                            task.Finish();
                            UpdateUI = true;
                        }
                        else//Some or all frames were missing dispite Log data.
                        {
                            task.taskFail("The worker returned task to Manager without completing it.");
                        }
                    }

                }
                else//blender render
                {
                    if (task.Status == 1)
                    {
                        if (CheckFiles(task))//Gets progress frome file existance, age and readabiliity.
                        {
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
                localProgress += task.FinishedFrameNumbers.Count() * ProgressPerFrame;//Adds progress from each tasks finished frames to local variable. 
                localFinishedFrames += task.FinishedFrameNumbers.Count();
            }

            if (localProgress > Progress)//If all tasks progress is grater than previous Progress report then Progress is updated. 
            {
                Progress = (float)Math.Round(localProgress);
                CompletedFrames = localFinishedFrames;
            }

            if (Progress >= 100 || renderTasks.All(t => t.Status == 2))//Job finished
            {
                JobsDone();
                return;
            }

            if (PauseRequest) Status = 3;//Pause
            else if (renderTasks.Any(t => t.Status == 1)) Status = 1;//Tasks are rendering
            else if (Status != 2 && Status != 3 && Status != 5)//No Tasks are rendering
            {
                if (StartTimes.Count() > EndTimes.Count() && DateTime.Now - StartTimes.Last() > TimeSpan.FromSeconds(5))
                {
                    EndTimes.Add(DateTime.Now);
                }

                if (Status != 4) Status = 0;
                Thread.Sleep(500);
            }
            else if (renderTasks.All(t => t.Status == 2)) Status = 2; //All tasks have finished.
        }
        public void JobsDone()
        {
            foreach (renderTask rt in renderTasks)
            {
                if (rt.Status != 2 || rt.progress < 100 || rt.finishTime == DateTime.MinValue)
                {
                    rt.Status = 2;
                    rt.progress = 100;
                    //rt.finished = true;
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
        public void TimeEstimate()//This will calculate the estimated remaining time.
        {
            double remainingProgress = 100 - Progress;
            TimeSpan TotalTime = TimeSpan.Zero;
            TimeSpan localMHours = TimeSpan.Zero;
            int count = 0;
            DateTime Earliest = DateTime.Now;
            DateTime EndTime = DateTime.MinValue;//j.renderTasks[0].lastUpdate;

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

            //timeDifferance = Latest - Earliest;
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
            try//This will probably need to be fixed, but it keeps thing from crashing.
            {
                RemainingTime = TimeSpan.FromSeconds(Math.Round(remainingProgress * averagePPS));
            }
            catch
            {
                RemainingTime = TimeSpan.Zero;
            }
        }
        public void SetOutputOffset()
        {
            //string Root = Path.GetPathRoot(outputDir);
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
            //int currentFrame = 0;
            //int AdjTaskframes = _rT.adjustedFrameRange;//FindAdjustedRange((_rT.finalFrame - _rT.FirstFrame), _j.FrameStep);
            //float taskPercnt = _rT.adjustedFrameRange / 100; //Convert.ToDouble((_rT.finalFrame - _rT.FirstFrame) + 1); ; 
            //float JobPercet = FindAdjustedRange(TotalFramesToRender, FrameStep) / 100;

            //TimeSpan timeOffset = TimeSpan.Zero;


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
                //replaceString = PoundString
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
            //int completedFrames = _rT.FinishedFrameNumbers.Count();
            bool done = true;
            foreach (var frameNum in UnfinishedFrames)
            {
                //Stopwatch sw = Stopwatch.StartNew();
                string filePathwithNum = "";


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
                        if (size >= SmallestFile && SmallestFile > 0)
                        {
                            //File is assumed good by comparing the smallest known good file to this file's size.  
                            //completedFrames++;
                            _rT.FinishedFrameNumbers.Add(frameNum);
                            _rT.taskLogs.WriteToManager($"{frameNum} Found, assumed good by size. \n");
                            //Console.WriteLine($"{frameNum} check time: {sw.ElapsedMilliseconds}");
                        }
                        else if (FileCheck.CheckFileReadability(filePathwithNum))
                        {
                            //File failed file size check, checking intergrity explicetly. This is FAR more disk intensive and would slow everything down consiterably if done to each file individually.
                            //Every task has a smallest file which is used for file check, if a file is smaller than the samllest its checked explicitly, if found good its size is used as the new smallest. 

                            _rT.taskLogs.WriteToManager($"{frameNum} Found, checked explicitly\n");

                            if (size > 1024)
                            {
                                SmallestFile = Convert.ToDouble(size);
                                _rT.taskLogs.WriteToManager($"Smallest file size set to: {Math.Round(size / 1024)}KB\n");
                            }


                            //}

                            //completedFrames++;
                            //_rT.adjustedFirstFrame = frameNum;
                            _rT.FinishedFrameNumbers.Add(frameNum);
                            //Console.WriteLine($"{frameNum} check time: {sw.ElapsedMilliseconds}");
                            //_rT.mangagerLog[_rT.attempt] += $"{frameNum} Found, checked explicitly\n";

                        }
                        else
                        {
                            _rT.taskLogs.WriteToManager($"{frameNum} Found, but failed integreity check.\n");
                            done = false;
                        }
                    }
                    else
                    {
                        done = false;
                        _rT.taskLogs.WriteToManager($"{frameNum} Old file was found, ignoring.\n");
                    }
                }
                else
                {
                    done = false;
                    _rT.taskLogs.WriteToManager($"{frameNum} Not found.\n");
                }
                //else File dosen't exist.
            }
            //Console.WriteLine("-------------------------------------------------------------------------");
            _rT.FinishedFrameNumbers.Sort();
            _rT.adjustedFirstFrame = 0;

            _rT.FindAdjustedFirstFrame();
            _rT.progress = (float)Math.Ceiling(_rT.FinishedFrameNumbers.Count() * _rT.ProgressPerFrame);
            //_rT.FinishedFrameCount = completedFrames;

            if (done) return true;
            else return false;
        }

        internal void BuiildOutputDir()
        {
            if(!Directory.Exists(outputDir)) 
            {
                Console.WriteLine("Output folder doesn't exists. Building...");
                try
                {
                    Logic.BuildFolderPath(outputDir);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Attempting to build folder path for new Job.\nERROR: \n" + e);
                }
                
            }
            else Console.WriteLine("Output folder exists.");



        }
        /*
public static int FindAdjustedRange(int Frames, int FrameStep)
{

   int Adjusted = 0;
   int Count = 0;

   while (Adjusted <= Frames)
   {
       //if (FrameStep == 0) return Frames;
       Count++;
       Adjusted += FrameStep;
   }
   return Count;

}

*         public void GetJobProgress()
{

   int LogIndex = 0;
   if (ProgressPerFrame == 0)
   {
       ProgressPerFrame = 100 / (float)TotalFramesToRender;
   }

   float BeginProgress = Progress;
   int localFinishedFrameCount = 0;
   //double jobPercent = 100 / Convert.ToDouble(TotalFramesToRender);
   float localPercent = 0;
   //DateTime fileAge = DateTime.Now;

   if (!Directory.Exists(outputDir)) return;
   if (vid)
   {
       GetAEVidJobProgress();
   }
   else
   {
       foreach (renderTask t in renderTasks)
       {
           LogIndex = t.Attempt();
           //if (t.Status == 0) continue;
           //Check files if a finish has been reported.

           //jobManager.CheckFiles(this, t);
           if (LogIndex < 0) return;
           if (t.Status == 0 || t.Status == 1 || (t.Status == 2 && t.progress < 100)) //Check Task for crash after task has been submitted for rendering.
           {
               //t.CheckTaskFinish(this);

               //if(t.Status == 1 && RenderApp =="ae") t.AELogtoProgress();

               if (CheckFiles(t))//Checks to make sure the files actually exist.
               {
                   t.progress = 100;
                   t.FinishedFrameCount = t.adjustedFrameRange;
                   t.FinishedFrameNumbers = t.RenderFrameNumbers.ToList();
                   t.Status = 2;
                   //t.finished = true;
                   t.finishTime = DateTime.Now;
                   UpdateUI = true;
               }
               else if (t.Status == 1 || (t.Status == 2 && t.progress < 100))
               {
                   t.CheckForFail(MaxRenderTime, ID);
               }

           }

           localFinishedFrameCount += t.FinishedFrameNumbers.Count();
           localPercent = (ProgressPerFrame * localFinishedFrameCount);
           if (Progress <= localPercent)//UpdateJob Progress
           {
               CompletedFrames = localFinishedFrameCount;
               localPercent = (float)Math.Round(localPercent);
               Progress = localPercent;
           }

       }
   }
   if (Progress >= 100 || renderTasks.All(t => t.Status == 2))//Job finished
   {
       JobsDone();
   }
   if (PauseRequest) Status = 3;//Pause
   else if (renderTasks.Any(t => t.Status == 1)) Status = 1;//Tasks are rendering
   else if (Status != 2 && Status != 3 && Status != 5)//No Tasks are rendering
   {
       if (StartTimes.Count() > EndTimes.Count() && DateTime.Now - StartTimes.Last() > TimeSpan.FromSeconds(5))
       {
           EndTimes.Add(DateTime.Now);
       }

       if (Status != 4) Status = 0;
       Thread.Sleep(500);
   }
   else if (renderTasks.All(t => t.Status == 2)) Status = 2; //All tasks have finished.

}
public void GetAEVidJobProgress()
{
   DateTime fileAge = DateTime.Now;
   Double BeginProgress = Progress;
   //TimeSpan timeOffset = TimeSpan.Zero;
   renderTask t = renderTasks[0];
   try
   {
       //timeOffset = jobManager.DriveTimeOffset[jobManager.Drives.FindIndex(d => d == Path.GetPathRoot(outputDir))];
       fileAge = File.GetLastWriteTime(outputPath) - OutputDeviceOffset;
   }
   catch
   {
       Console.WriteLine("Missing time offset.");
   }


   if (!Directory.Exists(outputDir)) return;
   //if (t.Status == 0) return;

   //Check files if a finish has been reported.
   if (t.Status == 1) //Check Task for crash after task has been submitted for rendering.
   {
       Status = 1;
       t.CheckForFail(MaxRenderTime, ID);
       t.AELogtoProgress();
   }
   //t.CheckTaskFinish(this);
   if (FileCheck.CheckFileReadability(outputPath) && fileAge > CreationTime)//Mark Job complete. t.FinishReported &&
   {
       if (t.progress < 100) t.progress = 100;
       t.FinishedFrameCount = t.adjustedFrameRange;
       t.Status = 2;
       //t.finished = true;
       t.finishTime = DateTime.Now;
       Status = 2;
       finished = true;
       Progress = 100;
       EndTimes.Add(DateTime.Now);
       RemainingTime = TimeSpan.Zero;
       CompletedFrames = TotalFramesToRender;
   }
   else if (t.FinishReported)
   {
       Logic.taskFail(ID, t.ID, t.taskLogs.WorkerIDs.Last(), "File was created with errors. Rerendering...");
   }
   Progress = (float)Math.Round(ProgressPerFrame * t.FinishedFrameCount);
   t.progress = Progress;
   CompletedFrames = t.FinishedFrameCount;

   if (!finished && Status != 5 && Status != 1)
   {
       if (StartTimes.Count() > EndTimes.Count() && DateTime.Now - StartTimes.Last() > TimeSpan.FromSeconds(5))
       {
           EndTimes.Add(DateTime.Now);
       }
       if (Status != 4) Status = 0;
   }
}
public Job shallowCopy()
{
   return (Job)this.MemberwiseClone();
}

public void GetFusionJobProgress()
{

}
public void GetBlenderJobProgress()
{
   double BeginProgress = Progress;
   int TotalCompletedFrames = 0;
   double jobPercent = 100 / Convert.ToDouble(TotalFramesToRender);
   double currentJobPercent = 0;
   //bool jobActive = false;
   DateTime fileAge = DateTime.Now;

   foreach (renderTask rt in renderTasks)
   {
       if (rt.Status == 0) continue;
       rt.CheckTaskFinish(this);
       if (rt.Status == 1) //Check Task for crash after task has been submitted for rendering.
       {
           rt.CheckForFail(MaxRenderTime, ID);
           if (jobManager.CheckFiles(this, rt))//Checks to make sure the files actually exist.
           {
               if (rt.progress < 100) rt.progress = 100;
               //rt.FinishedFrames = rt.adjustedFrameRange;
               rt.Status = 2;
               rt.finished = true;
               //rt.lastUpdate = DateTime.Now;
               rt.finishTime = DateTime.Now;
           }
           //jobActive = true;
           TotalCompletedFrames += rt.FinishedFrameNumbers.Count();
           if (vid) continue;
       }
       else if (rt.Status == 2)
       {
           TotalCompletedFrames += rt.FinishedFrameNumbers.Count();
       }
   }
   currentJobPercent = (jobPercent * TotalCompletedFrames);
   if (Progress <= currentJobPercent)//UpdateJob Progress
   {
       CompletedFrames = TotalCompletedFrames;
       currentJobPercent = Math.Round(currentJobPercent);
       Progress = currentJobPercent;

   }

   if (Progress >= 100 || renderTasks.All(rT => rT.finished == true))//Job finished
   {
       if (renderTasks.Any(t => t.progress < 100))
       {
           foreach (renderTask rt in renderTasks)
           {
               if (!rt.finished)
               {
                   rt.Status = 2;
                   rt.progress = 100;
                   rt.finished = true;
                   rt.finishTime = DateTime.Now;
               }
           }
       }
       Status = 2;
       finished = true;
       Progress = 100;
       EndTimes.Add(DateTime.Now);
       RemainingTime = TimeSpan.Zero;
       CompletedFrames = TotalFramesToRender;
       //jobActive = false;
   }

   if (renderTasks.Any(t => t.Status == 1))
   {
       Status = 1;
   }
   else if (Status != 2) Status = 0;
   else if (!finished && Status != 5)
   {
       if (StartTimes.Count() > EndTimes.Count())
       {
           EndTimes.Add(DateTime.Now);
       }
       Status = 0;
   }
}



public void BuildStatusThread()
{
   ProgressThread = new Thread(() =>
   {
       Console.WriteLine($"{Name} Status thread Started");
       while (Status == 1)
       {
           Thread.Sleep(5000);
           UpdateUI = true;
           Database.UpdateDBFile = true;
           try
           {
               switch (RenderApp.ToLower())
               {
                   case ("ae"):
                       if (vid) GetAEVidJobProgress(outputPath, CreationTime);
                       else GetAEJobProgress();
                       Logic.estimateRemainingTime(this);
                       break;

                   case ("blender"):
                       GetAEJobProgress();
                       Logic.estimateRemainingTime(this);
                       break;

                   case ("fusion"):
                       GetFusionJobProgress();
                       Logic.estimateRemainingTime(this);
                       break;
               }
           }
           catch
           {
               Console.WriteLine($"Failed to get progress on {Name} \nID: {ID}.");
           }
           if (fail)
           {
               Status = 4;
               break;
           }

       }
       UpdateUI = false;
       if (PauseRequest) Status = 3;
       Console.WriteLine($"{Name} Status thread closed");
   });
   ProgressThread.Name = Name + "_PThread";
   return;
}
/*
   switch (RenderApp.ToLower())
   {

       case ("ae"):
           ProgressThread = new Thread(() =>
           {
               Console.WriteLine($"{Name} Status thread Started");
               while (Status == 1)
               {
                   Thread.Sleep(100);
                   UpdateUI = true;
                   try
                   {
                       if (vid) GetAEVidJobProgress(outputPath, CreationTime);
                       else GetAEJobProgress();
                       Logic.estimateRemainingTime(this);
                   }
                   catch
                   {
                       Console.WriteLine($"Failed to get progress on {Name} \nID: {ID}.");
                   }
                   if (fail)
                   {
                       Status = 4;
                       break;
                   }
               }
               UpdateUI = false;
               Console.WriteLine($"{Name} Status thread closed");

           });
           ProgressThread.Name = Name + "_PThread";
           return;

       case ("blender"):
           ProgressThread = new Thread(() =>
           {
               Console.WriteLine($"{Name} Status thread Started");
               while (Status == 1)
               {
                   UpdateUI = false;
                   Thread.Sleep(500);
                   try
                   {
                       GetBlenderJobProgress();
                       Logic.estimateRemainingTime(this);
                   }
                   catch
                   {
                       Console.WriteLine($"Failed to get progress on {Name} \nID: {ID}.");
                   }

                   if (fail)
                   {
                       Status = 4;
                       break;
                   }
               }
               UpdateUI = false;
               Console.WriteLine($"{Name} Status thread closed");

           });
           ProgressThread.Name = Name + "_PThread";
           return;

       case ("fusion"):
           ProgressThread = new Thread(() =>
           {
               Console.WriteLine($"{Name} Status thread Started");
               while (Status == 1)
               {
                   Thread.Sleep(500);
                   try
                   {
                       GetFusionJobProgress();
                       Logic.estimateRemainingTime(this);
                   }
                   catch
                   {
                       Console.WriteLine($"Failed to get progress on {Name} \nID: {ID}.");
                   }
                   if (fail)
                   {
                       Status = 4;
                       break;
                   }

               }
               Console.WriteLine($"{Name} Status thread closed");
               if (fail) Status = 4;
           });
           ProgressThread.Name = Name + "_PThread";
           return;
       default:
           ProgressThread = null;
           return;
   }*/
    }

}
