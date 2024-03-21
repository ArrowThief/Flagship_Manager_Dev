using FlagShip_Manager.Objects;

namespace FlagShip_Manager.Helpers
{
    public class FSDebug
    {
        public bool DebugMode = true; //This should be false most of the time.
        private static int CurrentDummys = 0;
        public static void AddDummyWorker()
        {
            string pad = "00";
            if (CurrentDummys > 99) pad = "";
            else if (CurrentDummys > 9) pad = "0";
            var DummyName = $"Dummy{pad}{CurrentDummys}";
            WorkerObject DW = new WorkerObject();
            DW.name = DummyName;
            DW.GPU = false;
            DW.Status = 0;
            DW.Dummy = true;
            Random random = new Random();
            int newID = random.Next(9999, 99999);
            while (true)//Check for duplicated workerIDs
            {
                if (WorkerServer.WorkerList.Any(w => w.WorkerID == newID)) newID = random.Next(9999, 99999);
                else break;
            }
            DW.WorkerID = newID;
            WorkerServer.WorkerList.Add(DW);
            CurrentDummys++;
        }
        public static void AddLongDummyWorker()
        {
            string pad = "00";
            if (CurrentDummys > 99) pad = "";
            else if (CurrentDummys > 9) pad = "0";
            var DummyName = $"Long_Dummy-Name_Number_{pad}{CurrentDummys}";
            WorkerObject DW = new WorkerObject();
            DW.name = DummyName;
            DW.GPU = false;
            DW.Status = 0;
            DW.Dummy = true;
            Random random = new Random();
            int newID = random.Next(9999, 99999);
            while (true)//Check for duplicated workerIDs
            {
                if (WorkerServer.WorkerList.Any(w => w.WorkerID == newID)) newID = random.Next(9999, 99999);
                else break;
            }
            DW.WorkerID = newID;
            WorkerServer.WorkerList.Add(DW);
            CurrentDummys++;
        }
        public static void ClearrDummyWorkers()
        {
            List<int> RemoveIndex = new List<int>();
            for (int i = 0; i < WorkerServer.WorkerList.Count(); i++)
            {
                if (WorkerServer.WorkerList[i].Dummy)
                {
                    WorkerServer.WorkerList.RemoveAt(i);
                    i--;
                }
            }
            CurrentDummys = 0;
        }
    }
}
