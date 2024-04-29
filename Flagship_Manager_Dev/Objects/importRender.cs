using FlagShip_Manager.Management_Server;
using Flagship_Manager_Dev.Components;
using System.Text.Json;
using System.Web;

namespace FlagShip_Manager.Objects
{
    public class importRender
    {
        //Object matching output from After effects and Blender
        //TODO: Merge this into Job such that it doesn't need to exits as a seprate class.

        public string? Project { get; set; }
        public string? WorkingProject { get; set; }
        public string? Name { get; set; }
        public string? outputType { get; set; }
        public string? Filepath { get; set; }
        public float StartFrame { get; set; }
        public int FrameRange { get; set; }
        public int FrameStep { get; set; }
        public bool GPU { get; set; }
        public bool OW { get; set; }
        public bool vid { get; set; }
        public string? RenderApp { get; set; }
        public int Priority { get; set; }
        public int split { get; set; }
        public int QueueIndex { get; set; }

        public Job JsonToJob()
        {
            //Builds List of Jobs from an ImportJob object.
            
            Job newJob = new Job();
            bool[] inQueue = null;
            int count = 0;

            
            int TotalFrames = FrameRange;
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
            if (Name == "") newJob.Name = "Unsaved Project";
            else newJob.Name = Name;

            newJob.Project = Path.GetFullPath(HttpUtility.UrlDecode(Project));
            newJob.WorkingProject = WorkingProject;
            newJob.outputPath = Filepath;
            if (Filepath != "") newJob.outputDir = Directory.GetParent(Filepath).ToString();
            else newJob.outputDir = "";
            newJob.RenderPreset = outputType;
            newJob.FirstFrame = Convert.ToInt32(Math.Floor(StartFrame));
            if (FrameStep > 1)
            {
                newJob.FrameStep = FrameStep;
                newJob.TotalFramesToRender = Convert.ToInt32(decimal.Floor(FrameRange / FrameStep));
                newJob.FrameRange = newJob.TotalFramesToRender * FrameStep;
            }
            else
            {
                newJob.FrameStep = 1;
                newJob.TotalFramesToRender = FrameRange;
                newJob.FrameRange = FrameRange;
            }
            newJob.GPU = Convert.ToBoolean(GPU);
            newJob.FileFormat = Logic.GetRenderType(outputType, Filepath);
            if (RenderApp.ToLower() == "blender") newJob.Overwrite = true;
            else newJob.Overwrite = Convert.ToBoolean(OW);
            newJob.QueueIndex = QueueIndex;
            newJob.vid = vid;
            newJob.WorkerBlackList = new List<int>();
            newJob.RenderApp = RenderApp.ToLower();
            newJob.Status = 0;
            newJob.CreationTime = DateTime.Now;
            newJob.ID = DB.NextActive();
            newJob.renderTasks = GenerateSteppedTasks(newJob);
            newJob.ProgressPerFrame = 100 / (float)newJob.TotalFramesToRender;
            newJob.Priority = Priority;
            newJob.ProgressPerSecond = new List<double>();
            newJob.BuiildOutputDir();
            newJob.SetOutputOffset();
            
            return newJob;

        }

        private renderTask[] GenerateSteppedTasks(Job ParentJob)
        {
            //Generates renderTasks from Job data.
            //TODO: Move into Job class and rewrite. 

            //.FirstFrame, FrameRange, split, newJob.CreationTime, newJob.vid, newJob.FrameStep, newJob.ID

            if (vid) split = 1;
            Random rnd = new Random();
            List<renderTask> _return = new List<renderTask>();
            decimal AdjustedRange = decimal.Floor(ParentJob.FrameRange / ParentJob.FrameStep);
            decimal roundSplitRange = decimal.Floor(AdjustedRange / split);

            int differance = Convert.ToInt32(AdjustedRange) - (Convert.ToInt32(roundSplitRange) * split);
            int firstFrame = ParentJob.FirstFrame;
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
                frameRange = (AdjustedFrameCount * ParentJob.FrameStep);
                if (c == 0)
                {
                    nt.finalFrame = firstFrame + frameRange - ParentJob.FrameStep;
                    firstFrame += (AdjustedFrameCount * ParentJob.FrameStep);
                }
                else
                {
                    nt.finalFrame = firstFrame + frameRange - ParentJob.FrameStep;
                    firstFrame += (AdjustedFrameCount * ParentJob.FrameStep);
                }
                nt.GenerateFrameCount(ParentJob.FrameStep);
                nt.parentID = ParentJob.ID;
                nt.finishTime = DateTime.MinValue;
                nt.Status = 0;
                nt.ProgressPerFrame = 100 / (float)nt.RenderFrameNumbers.Count();

                _return.Add(nt);
            }
            return _return.ToArray();
        }
    }
}
