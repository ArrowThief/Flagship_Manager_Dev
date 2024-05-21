using FlagShip_Manager;
using Flagship_Manager_Dev.Components;
using FlagShip_Manager.Objects;
using Microsoft.AspNetCore.CookiePolicy;
using System.Security.Cryptography;
using FlagShip_Manager.Management_Server;

namespace Flagship_Manager_Dev.Objects
{
    public class TaskLogs
    {
        public List<string> WorkerLog { get; set; } = new List<string>();
        public List<int> WorkerIDs { get; set; } = new List<int>();
        public List<string> ManagerLog { get; set; } = new List<string>();
        public List<string> ArchiveLog { get; set; } = new List<string>();
        public List<int> LogLines { get; set; } = new List<int>();
        public List<DateTime> SubmitTime { get; set; } = new List<DateTime>();

        public void add()
        {
            //Adds a new index to each of the Lists, except the archive.

            WorkerLog.Add("");
            WorkerIDs.Add(-1);
            ManagerLog.Add("");
            LogLines.Add(0);
            SubmitTime.Add(DateTime.MaxValue);
        }
        public void removeLast()
        {
            //Removes Last Log.

            int IndexToRemove = Attempt();
            WorkerLog.RemoveAt(IndexToRemove);
            WorkerIDs.RemoveAt(IndexToRemove);
            ManagerLog.RemoveAt(IndexToRemove);
            LogLines.RemoveAt(IndexToRemove);
            SubmitTime.RemoveAt(IndexToRemove);
        }
        public void ClearLast()
        {
            //Clears last Log.

            int IndexToClear = Attempt();
            WorkerLog[IndexToClear] = "";
            WorkerIDs[IndexToClear] = -1;
            ManagerLog[IndexToClear] = "";
            LogLines[IndexToClear] = 0;
            SubmitTime[IndexToClear] = DateTime.MinValue;
        }
        public void ArhiveAndClear()
        {
            //Copies Log to the Archive and clears all other logs.
            //TODO: Implement log Archive in UI.

            ArchiveLog.AddRange(WorkerLog.ToArray());
            WorkerLog.Clear();
            WorkerIDs.Clear();
            ManagerLog.Clear();
            LogLines.Clear();
            SubmitTime.Clear();
        }
        public int Attempt(bool Index = true)
        {
            //Returns the current index of the Arrays, excluding the archive.

            if (WorkerLog.Count() == 0) return 0;
            else if (Index) return WorkerLog.Count() - 1;
            else return WorkerLog.Count();
        }
        public void WriteToWorker(string _Log, bool append = true)
        {
            //Appends string to the Last Index of Worker Log.

            if (append) WorkerLog[Attempt()] += _Log;
            else WorkerLog[Attempt()] = _Log;
        }
        public void WriteToManager(string _Log, bool append = true)
        {
            //Appends string to the Last Index of Manager Log.

            if (append) ManagerLog[Attempt()] += $"{DateTime.Now} - {_Log}";
            else ManagerLog[Attempt()] = _Log;
        }
        public void WriteID(int _ID)
        {
            //Writes int to the Last Index of Worker IDs

            WorkerIDs[Attempt()] = _ID;
        }
        public string CurrentWorker()
        {
            //Returns the worker name in the Last Index of Worker IDs

            var temp = DB.workers;
            if (WorkerLog.Count() < 1 || WorkerIDs.Last() == -1) return "";
            try
            {
                Worker? w = DB.FindWorker(WorkerIDs.Last());
                //if (w != null)
                return w.name;
                //else return "Unkown Worker";
                
            }
            catch(Exception ex)
            {
                string message = $"Start ERROR Log ------------------------------------------\nUnable to find worker, Searching for ID: {WorkerIDs.Last()}\nCurrent Worker List order: ";
                foreach (Worker w in DB.workers) message += $"\n{w.ID}";
                message += "End ERROR Log ------------------------------------------\n";
                WriteToManager(message);
                Console.WriteLine(message);

                return "Unknown Worker";
            }
        }
    }
}