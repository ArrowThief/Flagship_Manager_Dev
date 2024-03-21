using FlagShip_Manager.Management_Server;
using System.Net.Sockets;
using System.Text.Json;

namespace FlagShip_Manager.Objects
{
    public class tcpPacket
    {
        public string? command { get; set; }
        public int status { get; set; } = 0;//Job/Task status. queued(0), Rendering(1), finished(2) paused(3), fialed(4), canceled(5).
        public string[] arguments { get; set; } = new string[0];//Aruguments RenderType(0) RenderCommand(1), OutputPath(2), 
        public int senderID { get; set; } = 0000;
        public string[] Logs { get; set; } = { "No log Data." };
        public int LogLines { get; set; } = 0;

        public void Send(Socket _s)
        {
            try
            {
                if (arguments == null) arguments = new string[0];
                if (Logs == null) Logs = new string[0];
                if (senderID == 0) senderID = 1000;
                //var SerializedPacket = JsonSerializer.Serialize(_sendPacket);
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
            tcpPacket temp;
            if (_s.Connected)
            {
                //var test = Program.ClientID;

                byte[] recBuf = new byte[8192];
                try
                {
                    if (_s.Connected) _s.Receive(recBuf);
                    var decompressedBuffer = Misc.DeCompressArray(recBuf);
                    recBuf = decompressedBuffer;
                    //int lastIndex = Array.FindLastIndex(recBuf, b => b != 0);
                    //Array.Resize(ref recBuf, lastIndex + 1);
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

                }



            }
            return false;
        }
    }

}
