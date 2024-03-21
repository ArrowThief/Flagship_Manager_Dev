using FlagShip_Manager;
using Flagship_Manager_Dev.Components;
using FlagShip_Manager.Objects;
using Microsoft.AspNetCore.CookiePolicy;
using System.Security.Cryptography;

namespace Flagship_Manager_Dev.Objects
{
    public class TaskLogs
    {
        public List<string> WorkerLog { get; set; } = new List<string>();
        public List<int> WorkerIDs { get; set; } = new List<int>();
        public List<string> ManagerLog { get; set;} = new List<string>();
        public List<string> ArchiveLog { get; set; } = new List<string>();
        public List<int> LogLines { get; set; } = new List<int>();
        public List<DateTime> SubmitTime { get; set; } = new List<DateTime>();

        public void add() //Adds a new index to each of the Lists, except the archive.
        {
            WorkerLog.Add("");
            WorkerIDs.Add(-1);
            ManagerLog.Add("");
            LogLines.Add(0);
            SubmitTime.Add(DateTime.MaxValue);
        }
        public void removeLast()
        {
            int IndexToRemove = Attempt();
            WorkerLog.RemoveAt(IndexToRemove);
            WorkerIDs.RemoveAt(IndexToRemove);
            ManagerLog.RemoveAt(IndexToRemove);
            LogLines.RemoveAt(IndexToRemove);
            SubmitTime.RemoveAt(IndexToRemove);
        }
        public void ClearLast()
        {
            int IndexToClear = Attempt();
            WorkerLog[IndexToClear] = "";
            WorkerIDs[IndexToClear] = -1; 
            ManagerLog[IndexToClear] = "";
            LogLines[IndexToClear] = 0;
            SubmitTime[IndexToClear] = DateTime.MinValue;
        }
        public void ArhiveAndClear() //Copies Log to the Archive and clears all other logs.
        {
            ArchiveLog.AddRange(WorkerLog.ToArray());
            WorkerLog.Clear();
            WorkerIDs.Clear();
            ManagerLog.Clear();
            LogLines.Clear();
            SubmitTime.Clear();
        }
        public int Attempt(bool Index = true) //Returns the current index of the Arrays, excluding the archive.
        {
            if (WorkerLog.Count() == 0) return 0;
            else if (Index) return WorkerLog.Count() - 1;
            else return WorkerLog.Count();
        }
        public void WriteToWorker(string _Log, bool append = true) //Appends string to the Last Index of Worker Log.
        {
            if(append)WorkerLog[Attempt()] += _Log;
            else WorkerLog[Attempt()] = _Log;
        }
        public void WriteToManager(string _Log, bool append = true) //Appends string to the Last Index of Manager Log.
        {
           if(append) ManagerLog[Attempt()] += $"{DateTime.Now} - {_Log}";
           else ManagerLog[Attempt()] = _Log;
        }
        public void WriteID(int _ID) //Writes int to the Last Index of Worker IDs
        {
            WorkerIDs[Attempt()] = _ID;
        }
        public string CurrentWorker() //Attempts the find the name of the Last Index of Worker IDs
        {
            if (WorkerLog.Count() < 1 || WorkerIDs.Last() == -1) return "";
            //else if (WorkerIDs.Last() == -1) return "";
            string Name = WorkerServer.WorkerList.Find(w => w.WorkerID == WorkerIDs.Last()).name;
            if (Name != null && Name != "")
            {
                return Name;
            }
            else { return "Unknown Worker"; }
        }
        

        /*
         *public string WorkerIDToName(int IDIndex) //Attempts the find the workers name using the ID number at the passed index.
        {
            int ID = -1;
            string Name = "";
            try
            {
                ID = WorkerIDs[IDIndex];
                Name = WorkerServer.WorkerList.Find(w => w.WorkerID == ID).name;
            }
            catch 
            {
                return "";
            } 
            
            if (Name != null && Name != "")
            {
                return Name;
            }
            else { return "Unknown Worker"; }
        }
         * public string WorkerIDToName (int _ID) //Attempts the find the name using a passed Workers ID number. 
        {
            if (_ID == -1) return "";
            string Name = WorkerServer.WorkerList.Find(w => w.WorkerID == _ID).name;
            if(Name != null && Name != "")
            {
                return Name; 
            }
            else { return "Unknown Worker"; }
        }*/
    }

}
