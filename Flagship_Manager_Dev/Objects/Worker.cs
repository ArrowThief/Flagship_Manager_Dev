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
                ConsoleBuffer = "Couldn't find render task to cancel.";
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
        public tcpPacket? BuildResponse(tcpPacket _packet)
        {
            //builds a response tcpPacket to a recieved tcpPacket.
            //TODO: Remove references to Passive mode. 

            renderTask? rT = null;
            tcpPacket sendPacket = new tcpPacket();
            if (Status != 3) Status = _packet.status;
            if (activeJob != null)
            {
                //Get Current Logs from packet and pass them to correct renderTask and or Job. 

                try
                {
                    rT = activeJob.renderTasks[renderTaskIndex];
                    rT.lastUpdate = DateTime.Now;
                    if (_packet.command != "logpart" && _packet.arguments.Count() > 0)
                    {
                        if (_packet.arguments[0] != "log rebuild")
                        {
                            if (_packet.Logs.Count() > 0)
                            {

                                foreach (string log in _packet.Logs)
                                {
                                    if (rT.LastLogLine == log) continue;
                                    rT.taskLogs.WriteToWorker(log);
                                    rT.taskLogs.LogLines[rT.Attempt()]++;
                                }
                                rT.LastLogLine = _packet.Logs[_packet.Logs.Length - 1];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Worker has attempted to report on a Job that doesn't exist. \njob ID: {activeJob.ID}\nTask ID: {renderTaskIndex} \n{ex}");
                    WorkerServer.cancelWorker(this, false, false);
                }
            }

            if (_packet.command == null) return null;
            try
            {
                switch (_packet.command.ToLower())
                {
                    //Currently there are a total of 13 possible cases.
                    //TODO: Simplify by switching to int rather than strings. Less readable, but faster.

                    case "find":

                        //Responds to worker seaching for server IP.

                        sendPacket.command = "acknowledge";
                        return sendPacket;

                    case "setup":

                        //Builds new workerObject and adds it to List.

                        return newWorkerSetup(_packet); ;

                    case "dissconnect":

                        //Allows for disconnecting gracefully.
                        
                        ConsoleBuffer = "Disconnected.";
                        Status = 7;
                        return null;

                    case "busy":

                        //Worker isn't available to respond for some reason. 

                        rT.Status = 0;
                        rT.taskLogs.ClearLast();
                        break;

                    case "statusupdate":

                        //Default update packet.

                        //Sends the current number of logs recieved from the worker.
                        if (rT != null) sendPacket.LogLines = rT.taskLogs.LogLines.Last();

                        if (Status == 0 && !awaitUpdate)
                        {
                            ConsoleBuffer = $"{name} is awaiting work.";
                            sendPacket.command = "getupdate";
                        }
                        else if (awaitUpdate)
                        {
                            sendPacket.command = "acknowledge_me";
                        }
                        else if (Status == 2)
                        {
                            sendPacket.command = "acknowledge";
                        }
                        else if (activeJob != null && Status == 1)
                        {
                            if (activeJob.Status == 5)
                            {
                                sendPacket.command = "cancel";
                                return sendPacket;
                            }
                            else
                            {
                                sendPacket.command = "getupdate";
                            }
                        }
                        else
                        {
                            sendPacket.command = "getupdate";
                        }
                        break;

                    case "return":

                        //Task returned. State of task can be included in arguments
                        //TODO: cleanup to match new worker responses. 

                        if (rT != null)
                        {
                            if (_packet.arguments[0] == "fail")//Task failed hard enough that the worker was able to detect it, it will be auto blacklisted to make sure it doesn't happen again.
                            {
                                activeJob.WorkerBlackList.Add(ID);
                                rT.taskLogs.WriteToWorker($"{name} Has been added to the black list.");
                            }
                            else if (_packet.arguments[0] == "log rebuild")//Finishs building the Log buffer before applying the new log data. This likely consists of multiple log rebuild packets, but ends here.
                            {
                                foreach (var logLine in _packet.Logs)
                                {
                                    LogBuffer += logLine;
                                }
                                rT.taskLogs.WriteToWorker(LogBuffer + "\n-----Log rebuild complete-----\n", false);
                                rT.taskLogs.WriteToManager("Worker Log rebuit.");
                                LogBuffer = "";
                                LogCount = 0;
                            }
                            else //Checks if a log rebuild is needed. If statement also acts as a loop block so workers cant get stuck trying to rebuild broken logs forever.
                            {
                                if (_packet.LogLines != rT.taskLogs.LogLines.Last())
                                {
                                    LogBuffer = $"----Logs expected {_packet.LogLines}----\n----Logs recieved {rT.taskLogs.LogLines.Last()}----\n----Rebuilding Log from source----\n";
                                    sendPacket.command = "logrebuild";
                                    sendPacket.LogLines = 0;
                                    return sendPacket;
                                }
                                else rT.taskLogs.WriteToWorker($"\n----Logs expected {_packet.LogLines}----\n----Logs recieved {rT.taskLogs.LogLines.Last()}----\n");
                            }

                            rT.taskLogs.WriteToWorker($"\n\n{name} has returned this task to the manager.\n\n-------------------------------Worker Log end-------------------------------\n");
                            rT.FinishReported = true;
                            rT.FinishReportedTime = DateTime.Now;
                            renderTaskIndex = -1;
                            activeJob = null;
                        }
                        else
                        {
                            activeJob = null;
                            renderTaskIndex = -1;
                        }
                        LogBuffer = "";
                        LogCount = 0;
                        awaitUpdate = true;
                        sendPacket.command = "acknowledge_me";
                        packetBuffer = new tcpPacket();
                        break;

                    case "logpart":

                        //recieved part of the log rebuild, final part will be sent with a return command.

                        foreach (var logLine in _packet.Logs)
                        {
                            LogBuffer += logLine;
                        }
                        LogCount += _packet.Logs.Count();
                        sendPacket.command = "logrebuild";
                        sendPacket.LogLines = LogCount;
                        break;

                    case "acknowledge_me":

                        //Requests acknoledgement from worker.

                        sendPacket.command = "acknowledge";
                        awaitUpdate = false;
                        renderTaskIndex = -1;
                        activeJob = null;
                        break;

                    case "acknowledge":

                        //respond to Workers requst for acknoladgedgement.

                        sendPacket.command = "getupdate";
                        awaitUpdate = false;
                        break;

                    case "passive":

                        //Deprecated way to watch the servers log from a worker. Replaced by GUI.
                        break;

                    case "available":

                        //Worker is stating its status is avalable for work 
                        //Deprecated, status is set in status update. 

                        Status = 0;
                        ConsoleBuffer = $"{name} is once again available to render.";
                        sendPacket.command = "getupdate";
                        break;

                    case "sleeping":

                        //Sets worker to no longer recieve renderTasks, but remains connected.
                        //TODO: Reduce packet frequency to once every minute when sleeping.

                        Status = 3;
                        sendPacket.command = "sleep";
                        ConsoleBuffer = $"{name} is sleeping";
                        break;

                    case "failedpacketread":

                        //Failed to read packet, requests the same packet again. 

                        if (LastSentPacket != null) sendPacket = LastSentPacket;
                        else sendPacket.command = "getupdate";
                        break;

                    default:

                        //If no response can be built, assume read failed.

                        sendPacket.command = "failedPacketRead";
                        break;
                }
                return sendPacket;
            }
            catch
            {
                //TODO: Check if this is causing problems.
                return null;
            }
        }
        private tcpPacket newWorkerSetup(tcpPacket receivedPacket)
        {
            //creates new workerObject, adds it to lists and returns a response tcpPacket.
            //TODO: Rewrite ID creation to be a simpler itteration. 

            var temp = DB.workers;
            tcpPacket returnPacket = new tcpPacket();
            var search = DB.FindWorkerIndex(receivedPacket.senderID);

            if (search > -1)
            {
                //Worker exists in worker history
                Worker w = DB.workers[search];
                returnPacket.command = "acknowledge_me";
                returnPacket.arguments = new string[1];
                ID = w.ID;
                Status = 0;
                name = w.name;
                lastSeen = DateTime.Now;
                LastSentPacket = w.LastSentPacket;
                AvailableApps = w.AvailableApps;
                GPU = bool.Parse(receivedPacket.arguments[3]);
                DB.workers[search] = this;

            }
            else
            {
                //Worker has never connected or history was cleared.

                if (receivedPacket.senderID != -1 && search != null)
                {
                    //Checks if worker has an existing ID and if it is in use by another worker.

                    ID = receivedPacket.senderID;
                    returnPacket.command = "acknowledge";
                    returnPacket.arguments = new string[1];
                }
                else
                {
                    //Assigning new ID.


                    ID = DB.NextWorker();
                    returnPacket.command = "assignid";
                    returnPacket.arguments = new string[1];
                    returnPacket.arguments[0] = $"{ID}";
                }
                name = receivedPacket.arguments[0];
                BuildRenderAppList(JsonSerializer.Deserialize<string[]>(receivedPacket.arguments[1]), receivedPacket.arguments[2].ToLower());
                GPU = bool.Parse(receivedPacket.arguments[3]);
                lastSeen = DateTime.Now;
                DB.workers.Add(this);
                Console.WriteLine($"New Worker added to WorkerList");
                DB.UpdateDBFile = true;
            }

            
            return returnPacket;
        }
    }
}