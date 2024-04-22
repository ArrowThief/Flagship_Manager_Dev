using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace FlagShip_Manager
{
    internal class WorkerServer
    {
        //Stores workerObjects for every worker that has previously connected. 
        //Listens for and creates connection threads for new workers. 


        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Thread Cleanup = new Thread(() => clientCleanup());

        private static void clientCleanup()
        {
            //Disconnects workers who still have an active TCP connection, but haven't responded in more than 4 minutes. 
            //TODO: Look into this, it could have to do with the weird connection issues.

            while (true)
            {
                Thread.Sleep(1000);
                for (int i = 0; i < DB.WorkerList.Count(); i++)
                {
                    Worker w = DB.WorkerList[i];
                    if (w == null || w.Dummy) continue;
                    try
                    {
                        if (w.lastSeen.AddMinutes(4) < DateTime.Now && w.Status != 7)
                        {
                            //w.socket.Disconnect(false);
                            //WorkerList.Remove(w);
                            w.Status = 7;
                        }
                    }
                    catch
                    {
                        w.Status = 7;
                        //WorkerList.Remove(w);
                    }
                }
                DB.WorkerList = DB.WorkerList.DistinctBy(x => x.WorkerID).ToList();

            }
        }
        public static void setupWorkerServer()
        {
            Cleanup.Start();
            Readout.ReadoutBuffer = "Setting Up server";
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 32761));
            _serverSocket.Listen(100);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);


        }
        public static void AcceptCallback(IAsyncResult AR)
        {
            //Accept call back from workers and starts new worker thread.

            Socket socket = _serverSocket.EndAccept(AR);
            Thread SocketThread = new Thread(() => { SendRecieveLoop(socket); });
            SocketThread.Start();
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }
        private static void SendRecieveLoop(Socket _socket)
        {
            //Worker connection loop. While worker is connected loop will send and recieve data every second. 
            //TODO: Find bug that sometimes caues workers to get caught in a loop of connecting and disconnecting. 

            var WorkerList = DB.WorkerList;

            Worker worker = new Worker();
            tcpPacket? sendPacket = new tcpPacket();
            tcpPacket? recPacket = new tcpPacket();
            while (_socket.Connected)
            {
                if (!recPacket.recieve(_socket))
                {
                    Console.WriteLine($"Failed to read packet from {worker.name}");
                    sendPacket.senderID = 1;
                    sendPacket.command = "failedPacketRead";
                    sendPacket.status = 0;
                    sendPacket.Logs = new string[1];
                    sendPacket.Logs[0] = "";
                    sendPacket.arguments = new string[0];
                }
                worker.lastSeen = DateTime.Now;
                sendPacket = BuildResponse(recPacket, ref worker);
                try
                {
                    if (sendPacket == null) break;
                    if (sendPacket.command == null)
                    {
                        Console.WriteLine($"{worker.name} is being sent a blank command");
                    }
                    else if (worker.packetBuffer != null)//Check for any overide packets. This should be depreicated as it could cause problems.
                    {
                        sendPacket = worker.packetBuffer;
                        worker.packetBuffer = null;
                    }
                    else if (sendPacket.command == null) sendPacket.command = "getupdate";

                    sendPacket.Send(_socket);
                    worker.packetBuffer = null;
                    worker.LastSentPacket = sendPacket;

                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex);
                }
            }
            Console.WriteLine("Worker Dissconnect");

            if (worker.Status == 1)
            {
                string l = $"{worker.name} has dissconnected unexpectedly during a render. Its assigned task will be reclaimed.";
                worker.WorkerTaskFail(l);
                worker.LogBuffer = l;

            }
            else worker.LogBuffer = $"{worker.name} was not doing work, so no task needs to be reclaimed.";

            worker.Status = 7;
        }
        private static tcpPacket? BuildResponse(tcpPacket _packet, ref Worker _worker)
        {
            //builds a response tcpPacket to a recieved tcpPacket.
            //TODO: Remove references to Passive mode. 

            Job? j = null;
            renderTask? rT = null;
            tcpPacket sendPacket = new tcpPacket();
            if (_worker.Status != 3) _worker.Status = _packet.status;
            if (_worker.JobID != 0)
            {
                int JID = _worker.JobID;
                try
                {
                    j = jobManager.jobList.Find(jl => jl.ID == JID);
                    if (j == null)
                    {
                        //Job not found in Job list, check Job archive.
                        Console.WriteLine($"Worker has attempted to report on a Job that doesn't exist. \njob ID: {JID}\nTask ID: {_worker.renderTaskIndex}");
                        WorkerServer.cancelWorker(_worker, false, false);
                    }

                    rT = j.renderTasks[_worker.renderTaskIndex];
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
                                    //Console.Write(log);
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
                    Console.WriteLine($"Worker has attempted to report on a Job that doesn't exist. \njob ID: {JID}\nTask ID: {_worker.renderTaskIndex} \n{ex}");
                    WorkerServer.cancelWorker(_worker, false, false);
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
                        
                        return newWorkerSetup(_packet, ref _worker); ;

                    case "dissconnect": 
                        
                        //Allows for disconnecting gracefully.
                        
                        dissconnectClient(_worker);
                        return null;
                    case "busy":
                        
                        //Worker isn't available to respond for some reason. 
                        
                        rT.Status = 0;
                        rT.taskLogs.ClearLast();
                        break;
                    case "statusupdate": 
                        
                        //Default update packet.
                        
                        if (rT != null) sendPacket.LogLines = rT.taskLogs.LogLines.Last();//Sends the current number of logs recieved from the worker.
                        if (_worker.Status == 0 && !_worker.awaitUpdate)
                        {
                            _worker.ConsoleBuffer = $"{_worker.name} is awaiting work.";
                            sendPacket.command = "getupdate";
                        }
                        else if (_worker.awaitUpdate)
                        {
                            sendPacket.command = "acknowledge_me";
                        }
                        else if (_worker.Status == 2)
                        {
                            sendPacket.command = "acknowledge";
                        }
                        else if (j != null && _worker.Status == 1)
                        {
                            if (j.Status == 5)
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
                                j.WorkerBlackList.Add(_worker.WorkerID);
                                rT.taskLogs.WriteToWorker($"{_worker.name} Has been added to the black list.");
                            }
                            else if (_packet.arguments[0] == "log rebuild")//Finishs building the Log buffer before applying the new log data. This likely consists of multiple log rebuild packets, but ends here.
                            {
                                foreach (var logLine in _packet.Logs)
                                {
                                    _worker.LogBuffer += logLine;
                                }
                                rT.taskLogs.WriteToWorker(_worker.LogBuffer + "\n-----Log rebuild complete-----\n", false);
                                rT.taskLogs.WriteToManager("Worker Log rebuit.");
                                _worker.LogBuffer = "";
                                _worker.LogCount = 0;
                            }
                            else //Checks if a log rebuild is needed. If statement also acts as a loop block so workers cant get stuck trying to rebuild broken logs forever.
                            {
                                if (_packet.LogLines != rT.taskLogs.LogLines.Last())
                                {
                                    _worker.LogBuffer = $"----Logs expected {_packet.LogLines}----\n----Logs recieved {rT.taskLogs.LogLines.Last()}----\n----Rebuilding Log from source----\n";
                                    sendPacket.command = "logrebuild";
                                    sendPacket.LogLines = 0;
                                    return sendPacket;
                                }
                                else rT.taskLogs.WriteToWorker($"\n----Logs expected {_packet.LogLines}----\n----Logs recieved {rT.taskLogs.LogLines.Last()}----\n");
                            }

                            rT.taskLogs.WriteToWorker($"\n\n{_worker.name} has returned this task to the manager.\n\n-------------------------------Worker Log end-------------------------------\n");
                            rT.FinishReported = true;
                            rT.FinishReportedTime = DateTime.Now;
                            _worker.renderTaskIndex = -1;
                            _worker.JobID = 0;
                        }
                        else
                        {
                            _worker.JobID = 0;
                            _worker.renderTaskIndex = -1;
                        }
                        _worker.LogBuffer = "";
                        _worker.LogCount = 0;
                        _worker.awaitUpdate = true;
                        sendPacket.command = "acknowledge_me";
                        _worker.packetBuffer = new tcpPacket();
                        break;

                    case "logpart":

                        //recieved part of the log rebuild, final part will be sent with a return command.
                        
                        foreach (var logLine in _packet.Logs)
                        {
                            _worker.LogBuffer += logLine;
                        }
                        _worker.LogCount += _packet.Logs.Count();
                        sendPacket.command = "logrebuild";
                        sendPacket.LogLines = _worker.LogCount;
                        break;

                    case "acknowledge_me": 
                        
                        //Requests acknoledgement from worker.
                        
                        sendPacket.command = "acknowledge";
                        _worker.awaitUpdate = false;
                        _worker.renderTaskIndex = -1;
                        _worker.JobID = 0;
                        break;

                    case "acknowledge": 
                        
                        //respond to Workers requst for acknoladgedgement.
                        
                        sendPacket.command = "getupdate";
                        _worker.awaitUpdate = false;
                        break;

                    case "passive": 
                        
                        //Deprecated way to watch the servers log from a worker. Replaced by GUI.
                        
                        _worker.ConsoleBuffer = $"{_worker.name} Switching to passive mode.";
                        try
                        {
                            sendPacket.arguments = Readout.consoleBuffer.ToArray();

                        }
                        catch
                        {
                            Console.WriteLine($"Failed to build send console to {_worker.name}");
                        }
                        sendPacket.command = "passive";
                        break;

                    case "available":

                        //Worker is stating its status is avalable for work 
                        //Deprecated, status is set in status update. 

                        _worker.Status = 0;
                        _worker.ConsoleBuffer = $"{_worker.name} is once again available to render.";
                        sendPacket.command = "getupdate";
                        break;

                    case "sleeping":

                        //Sets worker to no longer recieve renderTasks, but remains connected.
                        //TODO: Reduce packet frequency to once every minute when sleeping.

                        _worker.Status = 3;
                        sendPacket.command = "sleep";
                        _worker.ConsoleBuffer = $"{_worker.name} is sleeping";
                        break;

                    case "failedpacketread":

                        //Failed to read packet, requests the same packet again. 

                        if (_worker.LastSentPacket != null) sendPacket = _worker.LastSentPacket;
                        else if (_worker != null) dissconnectClient(_worker);
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
        private static tcpPacket newWorkerSetup(tcpPacket receivedPacket, ref Worker _worker)
        {
            //creates new workerObject, adds it to lists and returns a response tcpPacket.
            //TODO: Rewrite ID creation to be a simpler itteration. 

            var WorkerList = DB.WorkerList;
            tcpPacket returnPacket = new tcpPacket();
            if (WorkerList.Any(w => w.WorkerID == receivedPacket.senderID))
            {
                //Worker exists in worker history

                _worker = DB.FindWorker(receivedPacket.senderID);
                returnPacket.command = "acknowledge_me";
                returnPacket.arguments = new string[1];
            }
            else 
            {
                //Worker has never connected or history was cleared.

                if (receivedPacket.senderID != -1 && !WorkerList.Any(w => w.WorkerID == receivedPacket.senderID))
                {
                    //Checks if worker has an existing ID and if it is in use by another worker.

                    _worker.WorkerID = receivedPacket.senderID;
                    returnPacket.command = "acknowledge";
                    returnPacket.arguments = new string[1];
                }
                else 
                {
                    //Assigning new ID.

                    Random random = new Random();
                    int newID = random.Next(9999, 99999);
                    while (true)
                    {
                        //Check for duplicated workerIDs

                        if (WorkerList.Any(w => w.WorkerID == newID)) newID = random.Next(9999, 99999);
                        else break;
                    }
                    _worker.WorkerID = newID;
                    returnPacket.command = "assignid";
                    returnPacket.arguments = new string[1];
                    returnPacket.arguments[0] = $"{_worker.WorkerID}";
                }
                _worker.name = receivedPacket.arguments[0];
                _worker.BuildRenderAppList(JsonSerializer.Deserialize<string[]>(receivedPacket.arguments[1]), receivedPacket.arguments[2].ToLower());
                _worker.GPU = bool.Parse(receivedPacket.arguments[3]);
                _worker.lastSeen = DateTime.Now;
                WorkerList.Add(_worker);
                Console.WriteLine($"New Worker added to WorkerList");
                DB.UpdateDBFile = true;
            }

            WorkerList = WorkerList.OrderBy(x => x.name).ToList<Worker>();
            return returnPacket;
        }
        internal static void dissconnectClient(Worker c)
        {
            //Graceful dissconnection. 

            c.ConsoleBuffer = "Disconnected.";
            c.Status = 7;
        }
        internal static void cancelWorker(Worker c, bool CancelJob, bool blackList)
        {
            //Request worker cancel currently active renderTask.

            if (c == null) return;
            var cancelPacket = new tcpPacket();
            
            Job j = DB.FindActive(c.JobID);
            renderTask rT = j.renderTasks[c.renderTaskIndex];
            if (blackList) j.WorkerBlackList.Add(c.WorkerID);
            rT.taskLogs.SubmitTime[rT.Attempt()] = DateTime.Now;
            if (!CancelJob && rT.Status == 1) rT.Status = 0;
            else rT.Status = 5;
            
            cancelPacket.command = "cancel";
            cancelPacket.arguments = new string[0];
            c.packetBuffer = cancelPacket;
            c.JobID = 0;
            c.renderTaskIndex = -1;
        }
        public static void killWorker(Worker _c)
        {
            //Shuts down worker software on client PC.

            _c.packetBuffer = new tcpPacket();
            _c.packetBuffer.command = "dissconnect";
            _c.packetBuffer.arguments = new string[0];
            Console.WriteLine($"{_c.name} has been shutdown.");
            Thread.Sleep(500);
        }

        internal static Worker Find(int v)
        {
            //Binary search though worker list.

            throw new NotImplementedException();
        }
    }
}
