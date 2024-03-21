using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace FlagShip_Manager
{
    internal class WorkerServer
    {
        //private static byte[] _buffer = new byte[4096];
        //public static List<WorkerHistoryObject> WorkerHistory = new List<WorkerHistoryObject>();
        public static List<WorkerObject> WorkerList = new List<WorkerObject>();
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static Thread Cleanup = new Thread(() => clientCleanup());

        private static void clientCleanup()
        {
            while (true)
            {
                Thread.Sleep(1000);
                for (int i = 0; i < WorkerList.Count(); i++)
                {
                    WorkerObject w = WorkerList[i];
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
                WorkerList = WorkerList.DistinctBy(x => x.WorkerID).ToList();

            }
        }
        public static void setupWorkerServer()
        {
            Cleanup.Start();
            Readout.ReadoutBuffer = "Setting Up server";
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 32761));
            _serverSocket.Listen(100); //Backlog is more about how many clients per second you have connected rather than how many total. If more than 5 connections are attempted at the same time then any beyond the 5th will fail and need to retry. 
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);


        }
        public static void AcceptCallback(IAsyncResult AR)//Accept call back and build worker info.
        {
            Socket socket = _serverSocket.EndAccept(AR);
            Thread SocketThread = new Thread(() => { SendRecieveLoop(socket); });
            SocketThread.Start();
            //socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client.socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }
        private static void SendRecieveLoop(Socket _socket)
        {

            WorkerObject worker = new WorkerObject();
            //worker.socket = _socket;
            tcpPacket? sendPacket = new tcpPacket();
            tcpPacket? recPacket = new tcpPacket();
            while (_socket.Connected)
            {
                //tcpPacket WorkerPacket;// = RecievePacket(_socket, worker.name);
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
                /*if(WorkerPacket.command == "failedpacketread")
                {
                    sendPacket = worker.PacketHistory[0];
                }else 
                if (worker.JobID != 0 && worker.Status == 0 && worker.lastSubmittion.AddSeconds(30) < DateTime.Now)
                {
                    Console.Write($"\n{worker.name} has an assigned job but claims to be idle\nTask: {worker.renderTaskID}");
                    sendPacket = new tcpPacket();
                    sendPacket.command = "acknowledge_me";
                }
                */
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
                    //if (worker.PacketHistory.Count > 10) worker.PacketHistory.RemoveAt(9);
                    //worker.PacketHistory.Add(sendPacket);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex);
                }
            }
            //worker.Status = 4;
            //After Worker dissconnect.
            Console.WriteLine("Worker Dissconnect");

            if (worker.Status == 1)
            {
                //Logic.taskFail(jobManager.jobList[JTindex[0]].ID, jobManager.jobList[JTindex[0]].renderTasks[JTindex[1]].ID, worker.WorkerID, "has dissconnected unexpectedly during a render. Its assigned task will be reclaimed.");
                worker.WorkerTaskFail("has dissconnected unexpectedly during a render. Its assigned task will be reclaimed.");

            }
            else worker.LogBuffer = $"{worker.name} was not doing work, so no task needs to be reclaimed.";

            worker.Status = 7;
            //WorkerList.Remove(worker);

        }
        private static tcpPacket? BuildResponse(tcpPacket _packet, ref WorkerObject _worker)
        {
            //bool Archived = false;
            Job? j = null;
            renderTask? rT = null;
            tcpPacket sendPacket = new tcpPacket();
            if (_worker.Status != 3) _worker.Status = _packet.status;
            //int attempParse = 0;
            if (_worker.JobID != 0)
            {
                int JID = _worker.JobID;
                int RTID = _worker.renderTaskID;
                try
                {
                    
                    j = jobManager.jobList.Find(jl => jl.ID == JID);
                    if (j == null)//Job not found in Job list, check Job archive.
                    {
                        Console.WriteLine($"Worker has attempted to report on a Job that doesn't exist. \njob ID: {JID}\nTask ID: {RTID}");
                        WorkerServer.cancelWorker(_worker, false, false);
                    }

                    rT = j.renderTasks.Find(task => task.ID == RTID);
                    rT.lastUpdate = DateTime.Now;
                    if (_packet.command != "logpart" && _packet.arguments.Count() > 0)
                    {
                        if (_packet.arguments[0] != "log rebuild")
                        {
                            if (_packet.Logs.Count() > 0 && rT.ID == _worker.renderTaskID)
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
                    Console.WriteLine($"Worker has attempted to report on a Job that doesn't exist. \njob ID: {JID}\nTask ID: {RTID} \n{ex}");
                    WorkerServer.cancelWorker(_worker, false, false);
                }
            }
            if (_packet.command == null) return null; 
            //Add try here?
            try
            {
                switch (_packet.command.ToLower()) //Currently there are a total of 13 cases, this could be simplifed by switching to status codes (int) rather than using strings. 
                {
                    case "find": //This is for clients to find the server.
                        sendPacket.command = "acknowledge";
                        return sendPacket;

                    case "setup": //this fills out the client data.
                        return newWorkerSetup(_packet, ref _worker); ;

                    case "dissconnect": //Allows for disconnecting gracefully.
                                        //if (rT != null) Logic.taskFail(j.ID, rT.ID, _worker, "Worker was shut down during render.");
                        dissconnectClient(_worker);
                        return null;
                    case "busy":
                        rT.Status = 0;
                        rT.taskLogs.ClearLast();
                        //rT.Worker[LogIndex] = "";
                        //rT.Log[LogIndex] = "";
                        break;
                    case "statusupdate": //Status update.
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

                    case "return":  //Task returned. State of task can be included in arguments

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
                            _worker.renderTaskID = 0;
                            _worker.JobID = 0;
                        }
                        else
                        {
                            _worker.JobID = 0;
                            _worker.renderTaskID = 0;
                        }
                        _worker.LogBuffer = "";
                        _worker.LogCount = 0;
                        _worker.awaitUpdate = true;
                        sendPacket.command = "acknowledge_me";
                        _worker.packetBuffer = new tcpPacket();
                        break;

                    case "logpart":
                        //A part of the log.
                        foreach (var logLine in _packet.Logs)
                        {
                            _worker.LogBuffer += logLine;
                        }
                        _worker.LogCount += _packet.Logs.Count();
                        sendPacket.command = "logrebuild";
                        sendPacket.LogLines = _worker.LogCount;
                        break;

                    case "acknowledge_me": //Ask Worker to acknoladge its existance.
                        sendPacket.command = "acknowledge";
                        _worker.awaitUpdate = false;
                        _worker.renderTaskID = 0;
                        _worker.JobID = 0;
                        break;

                    case "acknowledge": //respond to Workers requst for acknoladgedgement.
                        var debug = _packet.command; //This doens't do anything. Just exists to see what was in the packet.
                        sendPacket.command = "getupdate";
                        _worker.awaitUpdate = false;
                        break;

                    case "passive": //Deprecated way to watch the servers log from a worker. Replaced by GUI.
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
                        _worker.Status = 0;
                        _worker.ConsoleBuffer = $"{_worker.name} is once again available to render.";
                        sendPacket.command = "getupdate";
                        break;

                    case "sleeping":
                        _worker.Status = 3;
                        sendPacket.command = "sleep";
                        _worker.ConsoleBuffer = $"{_worker.name} is sleeping";
                        break;

                    case "failedpacketread":
                        if (_worker.LastSentPacket != null) sendPacket = _worker.LastSentPacket;
                        else if (_worker != null) dissconnectClient(_worker);
                        else sendPacket.command = "getupdate";
                        //else _worker.socket.Disconnect(false);
                        break;

                    default:
                        sendPacket.command = "failedPacketRead";
                        break;
                }
                return sendPacket;
            }
            catch 
            {
                return null;
            }
        }
        private static tcpPacket newWorkerSetup(tcpPacket receivedPacket, ref WorkerObject _worker)
        {
            //bool Existing = false;
            tcpPacket returnPacket = new tcpPacket();
            if (WorkerList.Any(w => w.WorkerID == receivedPacket.senderID))//Worker exists in worker history
            {
                //Existing = true;
                //_worker.Active(WorkerHistory.Find(w => w.WorkerID.ToString() == receivedPacket.senderID));
                //if (WorkerList.Any(w => w.WorkerID.ToString() == receivedPacket.senderID))
                //{
                //    WorkerList.Remove(WorkerList.Find(w => w.WorkerID.ToString() == receivedPacket.senderID));
                //}
                _worker = WorkerList.Find(w => w.WorkerID == receivedPacket.senderID);
                returnPacket.command = "acknowledge_me";
                returnPacket.arguments = new string[1];
            }
            else //Worker has never connected or history was cleared.
            {
                if (receivedPacket.senderID != -1 && !WorkerList.Any(w => w.WorkerID == receivedPacket.senderID))//Does worker have an existing ID? if so is it in use?
                {
                    _worker.WorkerID = receivedPacket.senderID;
                    returnPacket.command = "acknowledge";
                    returnPacket.arguments = new string[1];
                }
                else //No ID or existing ID. Assigning new ID.
                {
                    Random random = new Random();
                    int newID = random.Next(9999, 99999);
                    while (true)//Check for duplicated workerIDs
                    {
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
                //string defaultApp = receivedPacket.arguments[2].ToLower();
                //if (defaultApp == "ae" || defaultApp == "fusion" || defaultApp == "blender") _worker.defaultRenderType = defaultApp;
                _worker.lastSeen = DateTime.Now;
                WorkerList.Add(_worker);
                //WorkerHistory.Add(_worker.History());
                Console.WriteLine($"New Worker added to WorkerList");
                Database.UpdateDBFile = true;
            }

            WorkerList = WorkerList.OrderBy(x => x.name).ToList<WorkerObject>();
            return returnPacket;
        }
        internal static void dissconnectClient(WorkerObject c)
        {
            //c.socket.Disconnect(false);
            c.ConsoleBuffer = "Disconnected.";//Could add workers name here once implemented.
            c.Status = 7;
            //WorkerList.Remove(c);
        }
        internal static void cancelWorker(WorkerObject c, bool CancelJob, bool blackList)
        {
            //var jl = ;
            if (c == null) return;
            var cancelPacket = new tcpPacket();
            bool fullbreak = false;
            foreach (var j in jobManager.jobList)
            {
                foreach (var t in j.renderTasks)
                {

                    if (t.ID == c.renderTaskID)
                    {
                        if (blackList) j.WorkerBlackList.Add(c.WorkerID);
                        t.taskLogs.SubmitTime[t.Attempt()] = DateTime.Now;
                        if (!CancelJob && t.Status == 1) t.Status = 0;
                        else t.Status = 5;
                        fullbreak = true;
                        break;
                    }


                }
                if (fullbreak) break;
            }
            cancelPacket.command = "cancel";
            cancelPacket.arguments = new string[0];
            c.packetBuffer = cancelPacket;
            c.JobID = 0;
            c.renderTaskID = 0;
        }
        public static void killWorker(WorkerObject _c)
        {
            _c.packetBuffer = new tcpPacket();
            _c.packetBuffer.command = "dissconnect";
            _c.packetBuffer.arguments = new string[0];
            Console.WriteLine($"{_c.name} has been shutdown.");
            Thread.Sleep(500);
        }
    }
}

/*
 * public static void sendPacket(tcpPacket _sendPacket, Socket _socket)
        {
            //var utf8 = Encoding.UTF8;
            //JsonString = JsonSerializer.Serialize(_sendPacket);
            //byte[] sendBuffer = utf8.GetBytes(JsonString);
            try
            {
                if (_sendPacket.arguments == null) _sendPacket.arguments = new string[0];
                if (_sendPacket.Logs == null) _sendPacket.Logs = new string[0];
                if (_sendPacket.senderID == 0) _sendPacket.senderID = 1000;
                //var SerializedPacket = JsonSerializer.Serialize(_sendPacket);
                byte[] sendBuffer = JsonSerializer.SerializeToUtf8Bytes(_sendPacket);
                _socket.Send(sendBuffer, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fialed to send packet. \nERROR: " + ex);
            }
        }
 * public static void SendCallback(IAsyncResult AR)
        {
            Socket socekt = (Socket)AR.AsyncState;
            socekt.EndSend(AR);
        }
 * internal static void sleepWorker(WorkerObject c)
        {
            c.Status = 3;
            c.ConsoleBuffer = $"{c.name} is sleeping";
            c.packetBuffer = new tcpPacket();
            c.packetBuffer.command = "sleep";
            c.packetBuffer.arguments = new string[0];
        }
        internal static void wakeWorker(WorkerObject c)
        {
            Thread.Sleep(300);
            c.packetBuffer = new tcpPacket();
            c.packetBuffer.command = "wakeup";
            c.packetBuffer.arguments = new string[0];
        }
        private static tcpPacket? RecievePacket(Socket _socket, string WorkerName = "Missing Info")
        {

            try
            {
                byte[] recBuf = new byte[8192];
                int count = 0;
                tcpPacket? recPacket = null;
                _socket.Receive(recBuf);

                while (true)
                {
                    try
                    {
                        int lastIndex = Array.FindLastIndex(recBuf, b => b != 0);

                        Array.Resize(ref recBuf, lastIndex + 1);
                        if (recBuf.Length >= 8192) throw new Exception($"Packet is too large to transfer.\nSize:{recBuf.Length}");
                        recPacket = JsonSerializer.Deserialize<tcpPacket>(recBuf);
                        break;
                    }
                    catch
                    {
                        //Console.WriteLine($"Failed to read packet from {WorkerName}. Attempt: {count}\n{ex}");
                        if (count > 2) break;
                        count++;
                    }

                }
                if (recPacket == null) throw new Exception($"Could not read Packet from {WorkerName}");
                return recPacket;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());//Should probably have this written somewhere in the GUI.
                tcpPacket FailPacket = new tcpPacket();
                FailPacket.senderID = 1;
                FailPacket.command = "failedPacketRead";
                FailPacket.status = 0;
                FailPacket.Logs = new string[1];
                FailPacket.Logs[0] = ex.ToString();
                FailPacket.arguments = new string[0];
                return FailPacket;
            }

        }

       private static void ConfirmWorkerResponse(Job? _j, ref renderTask _t, ref WorkerObject _worker, int TotalLogLines)
       {
           int failCount = 0;

           while (true)
           {

               if (_t.progress >= 99 || _t.Status == 2)
               {
                   _worker.JobID = 0;
                   _worker.renderTaskID = 0;
                   break;
               }
               else if (failCount > 10)
               {
                   Logic.taskFail(_j.ID, _t.ID, _worker.WorkerID, "Worker lied about finishing the task.");
                   break;
               }
               else
               {
                   failCount++;
                   Thread.Sleep(250);
               }

           }
       }
       */