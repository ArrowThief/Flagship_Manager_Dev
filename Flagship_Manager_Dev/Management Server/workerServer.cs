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
            //TODO: Remove this.

            while (true)
            {
                Thread.Sleep(1000);
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
                DB.workers = DB.workers.DistinctBy(x => x.ID).ToList();

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
                sendPacket = worker.BuildResponse(recPacket);
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

        internal static Worker Find(int v)
        {
            //Binary search though worker list.

            throw new NotImplementedException();
        }
    }
}
