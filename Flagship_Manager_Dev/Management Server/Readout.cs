namespace FlagShip_Manager
{
    internal class Readout
    {
        public static string ReadoutBuffer = "";
        public static List<string> consoleBuffer = new List<string>();
        internal static int currentJobItems = 0;
        internal static bool ConsoleWrite = true;

        /*public static void ConsoleWriter()
        {
            Console.CursorVisible = false;
            var Cs = WorkerServer.WorkerList;
            var Jl = jobManager.jobList;
            int LastwriteIndex = 1;
            //int writerIndex = 1;
            int lineCount = 0;
            //string lastWriteString = "";
            Stopwatch LastWrite = new Stopwatch();
            while (true)
            {
                if (ConsoleWrite)
                {
                    try
                    {
                        lineCount = 0;
                        int startPosition = Console.GetCursorPosition().Left;
                        if (Console.CursorVisible) Console.CursorVisible = false;
                        consoleBuffer.Clear();
                        consoleBuffer.Add(ReadoutBuffer);

                        foreach (Job j in Jl)//Add job console buffers to master console buffer.
                        {
                            if (j.finished || j.Status == 5) continue;
                            consoleBuffer.Add(j.ConsoleBuffer);
                        }
                        foreach (WorkerObject c in Cs)//Add client consolebuffers to master console buffer.
                        {
                            if (c == null || c.Status == 8) continue;//Check if client is broken or if client is passive
                            consoleBuffer.Add(c.ConsoleBuffer);
                        }
                        if (consoleBuffer.Contains("null"))
                        {
                            int[] remove = new int[consoleBuffer.Count];
                            for(int i = 0;  i < consoleBuffer.Count(); i++)
                            {
                                if (consoleBuffer[i] == null)
                                {
                                    consoleBuffer.RemoveAt(i);//remove null entries.
                                    i--;
                                }
                            }
                        }
                        foreach (string buff in consoleBuffer )//Write consoleBuffer to console.
                        {
                            Console.SetCursorPosition(0, lineCount);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, lineCount);
                            if(buff != null)Console.WriteLine(buff.Trim());
                            lineCount++;
                        }
                        
                        if(lineCount < LastwriteIndex)//Remove unneeded lines in the console.
                        {
                            int differance = LastwriteIndex - lineCount;
                            for(int i = 0; i < differance; i++)
                            {
                                Console.SetCursorPosition(0, lineCount +i);
                                Console.WriteLine(new String(' ', Console.WindowWidth));
                            }
                        }
                        Console.SetCursorPosition(startPosition, lineCount+1);
                        Thread.Sleep(500);
                        LastwriteIndex = lineCount;
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }
        */

    }
}
