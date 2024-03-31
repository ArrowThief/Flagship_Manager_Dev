using FlagShip_Manager.Management_Server;
using System.Net.Sockets;
using System.Text.Json;

namespace FlagShip_Manager.Objects
{
    public class tcpPacket
    {
        //Object used for sending and reciving data from Worker Clients
        //TODO: Rename, possibly workerPacket, or wPacket.

        public string? command { get; set; }
        public int status { get; set; } = 0;//Job/Task status. queued(0), Rendering(1), finished(2) paused(3), fialed(4), canceled(5).
        public string[] arguments { get; set; } = new string[0]; 
        public int senderID { get; set; } = 0000;
        public string[] Logs { get; set; } = { "No log Data." };
        public int LogLines { get; set; } = 0;

        public void Send(Socket _s)
        {
            //Compresses and sends packet to passed socket.
            try
            {
                if (arguments == null) arguments = new string[0];
                if (Logs == null) Logs = new string[0];
                if (senderID == 0) senderID = 1000;
                byte[] sendBuffer;
                byte[] CompressBufer;

                sendBuffer = JsonSerializer.SerializeToUtf8Bytes(this);
                JsonSerializer.Deserialize<tcpPacket>(sendBuffer);
                CompressBufer = Misc.CompressArray(sendBuffer);
                sendBuffer = CompressBufer;
                if (sendBuffer.Length > 8192) throw new Exception($"Attempting to send packet that is too large. \nSize:{sendBuffer.Length}");
                _s.Send(sendBuffer, SocketFlags.None);


            }
            catch (Exception ex)
            {
                Console.WriteLine("Fialed to send packet. \nERROR: " + ex);
            }
        }
        public bool recieve(Socket _s)
        {
            //Recieves and decompresses data from passed socket. Returns true if send is sucsessful.

            tcpPacket temp;
            if (_s.Connected)
            {
                //Socket is conenctd.

                byte[] recBuf = new byte[8192];
                try
                {
                    if (_s.Connected) _s.Receive(recBuf);
                    var decompressedBuffer = Misc.DeCompressArray(recBuf);
                    recBuf = decompressedBuffer;
                    temp = JsonSerializer.Deserialize<tcpPacket>(recBuf);
                    command = temp.command;
                    status = temp.status;
                    arguments = temp.arguments;
                    senderID = temp.senderID;
                    Logs = temp.Logs;
                    LogLines = temp.LogLines;
                    return true;
                }
                catch
                {
                    //Not much I can do from here.
                }

            }
            return false;
        }
    }

}
