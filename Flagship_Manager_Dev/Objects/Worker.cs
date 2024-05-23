using FlagShip_Manager.Management_Server;
using System.Text.Json;

namespace FlagShip_Manager.Objects
{
    public class Worker
    {
        //TCPIP Client object. Stores and controlls clients, ususally refered to as Workers.

        public int ID { get; set; } = 0;
        public string name { get; set; } = "null";
        public Job? activeJob { get; set; }
        public int renderTaskIndex { get; set; }
        public int Status { get; set; }//Client status. ready for work(0), rendering(1), complete(2), Paused/asleep(3), failed(4), canceled(5) starting up(6), offline(7), passive(8) asleep(9).
        public tcpPacket? packetBuffer { get; set; }
        public tcpPacket? LastSentPacket { get; set; }
        public bool awaitUpdate { get; set; }
        public bool GPU { get; set; } = false;
        public List<RenderApp> AvailableApps { get; set; } = new List<RenderApp>();
        //public string ConsoleBuffer { get; set; } = "";
        public DateTime lastSeen { get; set; }
        public DateTime lastSubmittion { get; set; } = DateTime.MinValue;
        public bool Dummy { get; set; } = false;
        public string LogBuffer { get; set; } = "";
        public int LogCount { get; set; } = 0;
        
        public void BuildRenderAppList(string[] _AppList, string _Default)
        {
            //Builds worker app list.

            RenderApp BuildApp = new RenderApp();
            foreach (string App in _AppList)
            {
                if (App == null) continue;
                BuildApp = new RenderApp();
                switch (App.ToLower())
                {
                    case ("ae"):
                        BuildApp.AppName = "ae";
                        BuildApp.ImagePath = "Images/App/ae.png";
                        if (_Default == "ae") BuildApp.Default = true;
                        break;
                    case ("blender"):
                        BuildApp.AppName = "blender";
                        BuildApp.ImagePath = "Images/App/blender.png";
                        if (_Default == "blender") BuildApp.Default = true;
                        break;
                    case ("fusion"):
                        BuildApp.AppName = "fusion";
                        BuildApp.ImagePath = "Images/App/fusion.png";
                        if (_Default == "fusion") BuildApp.Default = true;
                        break;
                    default:
                        continue;

                }
                AvailableApps.Add(BuildApp);
            }
        }
        public void SleepWorker()
        {
            //Puts worker in a sleep state, the client software remains active but no tasks will be sent while in this state.

            LogBuffer = "Sleeping worker.";
            foreach (RenderApp RA in AvailableApps)
            {
                RA.EnableDisable();
            }
            if (Status == 1)
            {
                LogBuffer = "Sleeping worker.";
                WorkerTaskFail("Worker being sent to sleep.", true);    
            }
            if (Status != 3) Status = 3;
            else Status = 0;
        }
        public void KillWorker()
        {
            //Shuts down client software on worker PC.

            LogBuffer = "Killing Worker.";
            packetBuffer = new tcpPacket();
            packetBuffer.command = "dissconnect";
            packetBuffer.arguments = new string[0];
            Console.WriteLine($"{name} has been shutdown.");
            if (Status == 1)
            {
                WorkerTaskFail($"{name} has been shutdown during a render. Task will be reclaimed.");
            }
        }
        public renderTask? RenderTask()
        {
            //Returns currently assigned renderTask
            if (activeJob != null)
            {
                return activeJob.renderTasks[renderTaskIndex];
            }
            else return null;
        }
        public void WorkerTaskFail(string ErrorLog, bool cancel = false, bool IgnoreAttempts = false)
        {
            //Attempts to run RenderTask Fail.
            //TODO: RenderTask should probably be a refrence.

            try
            {
                RenderTask().taskFail(ErrorLog, cancel, IgnoreAttempts);
            }
            catch
            {
                LogBuffer = "Couldn't find render task to cancel.";
            }
        }
        public void sendTasktoClientBuffer(Job j, renderTask t, int index)
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

            renderTaskIndex = index;
            activeJob = j;
            packetBuffer = sendPacket;
            Status = 1;
        }
    }
}