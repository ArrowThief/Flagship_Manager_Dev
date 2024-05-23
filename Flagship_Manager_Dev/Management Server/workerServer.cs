using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Xml.Linq;

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
            //TODO: Remove this.

            while (true)
            {
                Thread.Sleep(10000);
                for (int i = 0; i < DB.workers.Count(); i++)
                {
                    Worker w = DB.workers[i];
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
            }
        }
        public static void setupWorkerServer()
        {
            Cleanup.Start();
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

            var WorkerList = DB.workers;

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
                sendPacket = BuildResponse(recPacket, worker);
                try
                {
                    if (sendPacket == null)
                    {
                        _socket.Disconnect(true);
                        Console.WriteLine("Worker failed to send readable data.");
                        break;
                    }
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

            if (worker.Status == 1)
            {
                string l = $"{worker.name} has disconnected unexpectedly during a render. Its assigned task will be reclaimed.";
                worker.WorkerTaskFail(l);
                worker.LogBuffer = "Disconnected unexpectedly during a render. Assigned task will be reclaimed.";

            }
            else worker.LogBuffer = "Disconnected"; ;

            worker.Status = 7;
        }   
        internal static void cancelWorker(Worker c, bool CancelJob, bool blackList)
        {
            //Request worker cancel currently active renderTask.

            if (c == null) return;
            else if (c.activeJob == null) return;
            var cancelPacket = new tcpPacket();
            
            renderTask rT = c.activeJob.renderTasks[c.renderTaskIndex];
            if (blackList) c.activeJob.WorkerBlackList.Add(c.ID);
            rT.taskLogs.SubmitTime[rT.Attempt()] = DateTime.Now;
            if (!CancelJob && rT.Status == 1) rT.Status = 0;
            else rT.Status = 5;
            
            cancelPacket.command = "cancel";
            cancelPacket.arguments = new string[0];
            c.packetBuffer = cancelPacket;
            c.activeJob = null;
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
        private static tcpPacket? BuildResponse(tcpPacket _packet, Worker worker)
        {
            //builds a response tcpPacket to a recieved tcpPacket.
            //TODO: Remove references to Passive mode. 

            renderTask? rT = null;
            tcpPacket sendPacket = new tcpPacket();
            if (worker.Status != 3) worker.Status = _packet.status;
            if (worker.activeJob != null)
            {
                //Get Current Logs from packet and pass them to correct renderTask and or Job. 

                try
                {
                    rT = worker.activeJob.renderTasks[worker.renderTaskIndex];
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
                    Console.WriteLine($"Worker has attempted to report on a Job that doesn't exist. \njob ID: {worker.activeJob.ID}\nTask ID: {worker.renderTaskIndex} \n{ex}");
                    cancelWorker(worker, false, false);
                }
            }

            if (_packet.command == null) return null;
            try
            {
                switch (_packet.command.ToLower())
                {
                    //Currently there are a total of 13 possible cases.
                    //TODO: Simplify by switching to int rather than strings. Less human readable, but a little cleaner.

                    case "find":

                        //Responds to worker seaching for server IP.

                        sendPacket.command = "acknowledge";
                        return sendPacket;

                    case "setup":

                        //Builds new workerObject and adds it to List.

                        return newWorkerSetup(_packet, worker); ;

                    case "dissconnect":

                        //Allows for disconnecting gracefully.

                        worker.LogBuffer = "Disconnected.";
                        worker.Status = 7;
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

                        if (worker.Status == 0 && !worker.awaitUpdate)
                        {
                            worker.LogBuffer = $"Awaiting work.";
                            sendPacket.command = "getupdate";
                        }
                        else if (worker.awaitUpdate)
                        {
                            sendPacket.command = "acknowledge_me";
                        }
                        else if (worker.Status == 2)
                        {
                            sendPacket.command = "acknowledge";
                        }
                        else if (worker.activeJob != null && worker.Status == 1)
                        {
                            if (worker.activeJob.Status == 5)
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
                                worker.activeJob.WorkerBlackList.Add(worker.ID);
                                rT.taskLogs.WriteToWorker($"{worker.name} Has been added to the black list.");
                            }
                            else if (_packet.arguments[0] == "log rebuild")//Finishs building the Log buffer before applying the new log data. This likely consists of multiple log rebuild packets, but ends here.
                            {
                                foreach (var logLine in _packet.Logs)
                                {
                                    worker.LogBuffer += logLine;
                                }
                                rT.taskLogs.WriteToWorker(worker.LogBuffer + "\n-----Log rebuild complete-----\n", false);
                                rT.taskLogs.WriteToManager("Worker Log rebuit.");
                                worker.LogBuffer = "";
                                worker.LogCount = 0;
                            }
                            else //Checks if a log rebuild is needed. If statement also acts as a loop block so workers cant get stuck trying to rebuild broken logs forever.
                            {
                                if (_packet.LogLines != rT.taskLogs.LogLines.Last())
                                {
                                    worker.LogBuffer = $"----Logs expected {_packet.LogLines}----\n----Logs recieved {rT.taskLogs.LogLines.Last()}----\n----Rebuilding Log from source----\n";
                                    sendPacket.command = "logrebuild";
                                    sendPacket.LogLines = 0;
                                    return sendPacket;
                                }
                                else rT.taskLogs.WriteToWorker($"\n----Logs expected {_packet.LogLines}----\n----Logs recieved {rT.taskLogs.LogLines.Last()}----\n");
                            }

                            rT.taskLogs.WriteToWorker($"\n\n{worker.name} has returned this task to the manager.\n\n-------------------------------Worker Log end-------------------------------\n");
                            rT.FinishReported = true;
                            rT.FinishReportedTime = DateTime.Now;
                            worker.renderTaskIndex = -1;
                            worker.activeJob = null;
                        }
                        else
                        {
                            worker.activeJob = null;
                            worker.renderTaskIndex = -1;
                        }
                        worker.LogBuffer = "";
                        worker.LogCount = 0;
                        worker.awaitUpdate = true;
                        sendPacket.command = "acknowledge_me";
                        worker.packetBuffer = new tcpPacket();
                        break;

                    case "logpart":

                        //recieved part of the log rebuild, final part will be sent with a return command.

                        foreach (var logLine in _packet.Logs)
                        {
                            worker.LogBuffer += logLine;
                        }
                        worker.LogCount += _packet.Logs.Count();
                        sendPacket.command = "logrebuild";
                        sendPacket.LogLines = worker.LogCount;
                        break;

                    case "acknowledge_me":

                        //Requests acknoledgement from worker.

                        sendPacket.command = "acknowledge";
                        worker.awaitUpdate = false;
                        worker.renderTaskIndex = -1;
                        worker.activeJob = null;
                        break;

                    case "acknowledge":

                        //respond to Workers requst for acknoladgedgement.

                        sendPacket.command = "getupdate";
                        worker.awaitUpdate = false;
                        break;

                    case "passive":

                        //Deprecated way to watch the servers log from a worker. Replaced by GUI.
                        break;

                    case "available":

                        //Worker is stating its status is avalable for work 
                        //Deprecated, status is set in status update. 

                        worker.Status = 0;
                        worker.LogBuffer = $"Available to render.";
                        sendPacket.command = "getupdate";
                        break;

                    case "sleeping":

                        //Sets worker to no longer recieve renderTasks, but remains connected.
                        //TODO: Reduce packet frequency to once every minute when sleeping.

                        worker.Status = 3;
                        sendPacket.command = "sleep";
                        worker.LogBuffer = $"Sleeping";
                        break;

                    case "failedpacketread":

                        //Failed to read packet, requests the same packet again. 

                        if (worker.LastSentPacket != null) sendPacket = worker.LastSentPacket;
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
        private static tcpPacket newWorkerSetup(tcpPacket receivedPacket, Worker worker)
        {
            //creates new workerObject, adds it to lists and returns a response tcpPacket.

            var temp = DB.workers;
            tcpPacket returnPacket = new tcpPacket();
            worker.name = receivedPacket.arguments[0];
            worker.lastSeen = DateTime.Now;
            worker.GPU = bool.Parse(receivedPacket.arguments[3]);
            worker.BuildRenderAppList(JsonSerializer.Deserialize<string[]>(receivedPacket.arguments[1]), receivedPacket.arguments[2].ToLower());

            var search = DB.FindWorkerIndex(receivedPacket.senderID);
           
            if (search != -1 && DB.workers[search].name == worker.name)
            {
                //Worker has an ID already.

                worker.LogBuffer = "Reconnected";
                returnPacket.command = "acknowledge_me";
                returnPacket.arguments = new string[0];
                worker.ID = receivedPacket.senderID;
                DB.workers[search] = worker;
            }
            else
            {
                //Worker will be assigned a new ID.

                worker.LogBuffer = "New worker connected";
                returnPacket.command = "assignid";
                DB.AddToWorkers(worker);
                returnPacket.arguments = new string[] {$"{worker.ID}"};
                
                
            }

            DB.UpdateDBFile = true;
            return returnPacket;
        }
    }
}
