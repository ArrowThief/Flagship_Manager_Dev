namespace FlagShip_Manager.Objects
{
    public class WorkerObject
    {
        //TCPIP Client object. Stores and controlls clients, ususally refered to as Workers.

        public string name { get; set; } = "null";
        public int WorkerID { get; set; } = 0000;
        public int JobID { get; set; }
        public int renderTaskID { get; set; }
        public int Status { get; set; }//Client status. ready for work(0), rendering(1), complete(2), Paused/asleep(3), failed(4), canceled(5) starting up(6), offline(7), passive(8) asleep(9).
        public tcpPacket? packetBuffer { get; set; }
        public tcpPacket? LastSentPacket { get; set; }
        public bool awaitUpdate { get; set; }
        public bool GPU { get; set; } = false;
        public List<RenderApp> AvailableApps { get; set; } = new List<RenderApp>();
        public string ConsoleBuffer { get; set; } = "";
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

            ConsoleBuffer = "Sleeping worker.";
            foreach (RenderApp RA in AvailableApps)
            {
                RA.EnableDisable();
            }
            if (Status == 1)
            {
                ConsoleBuffer = "Sleeping worker.";
                WorkerTaskFail("Worker being sent to sleep.", true);    
            }
            if (Status != 3) Status = 3;
            else Status = 0;
        }
        public void KillWorker()
        {
            //Shuts down client software on worker PC.

            ConsoleBuffer = "Killing Worker.";
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
            //TODO: Make refrence.

            renderTask? rT = jobManager.jobList.Find(j => j.ID == JobID).renderTasks.Find(rT => rT.ID == renderTaskID);
            return rT;
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
                ConsoleBuffer = "Couldn't find render task to cancel.";
            }
        }
    }
}