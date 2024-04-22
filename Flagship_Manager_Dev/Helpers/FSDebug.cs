using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;

namespace FlagShip_Manager.Helpers
{
    public class FSDebug
    {
        //Debugging class

        public bool DebugMode = true; //This should be false most of the time.
        private static int CurrentDummys = 0;
        public static void AddDummyWorker()
        {
            //Creates a fake worker to test UI.
            string pad = "00";
            if (CurrentDummys > 99) pad = "";
            else if (CurrentDummys > 9) pad = "0";
            var DummyName = $"Dummy{pad}{CurrentDummys}";
            Worker DW = new Worker();
            DW.name = DummyName;
            DW.GPU = false;
            DW.Status = 0;
            DW.Dummy = true;
            Random random = new Random();
            int newID = random.Next(9999, 99999);
            while (true)//Check for duplicated workerIDs
            {
                if (DB.WorkerList.Any(w => w.WorkerID == newID)) newID = random.Next(9999, 99999);
                else break;
            }
            DW.WorkerID = newID;
            DB.WorkerList.Add(DW);
            CurrentDummys++;
        }
        public static void AddLongDummyWorker()
        {
            //Creates a fake worker wtih a very long name to test UI.

            string pad = "00";
            if (CurrentDummys > 99) pad = "";
            else if (CurrentDummys > 9) pad = "0";
            var DummyName = $"Long_Dummy-Name_Number_{pad}{CurrentDummys}";
            Worker DW = new Worker();
            DW.name = DummyName;
            DW.GPU = false;
            DW.Status = 0;
            DW.Dummy = true;
            Random random = new Random();
            int newID = random.Next(9999, 99999);
            while (true)//Check for duplicated workerIDs
            {
                if (DB.WorkerList.Any(w => w.WorkerID == newID)) newID = random.Next(9999, 99999);
                else break;
            }
            DW.WorkerID = newID;
            DB.WorkerList.Add(DW);
            CurrentDummys++;
        }
        public static void ClearrDummyWorkers()
        {
            //Removes all dummy workers.

            List<int> RemoveIndex = new List<int>();
            for (int i = 0; i < DB.WorkerList.Count(); i++)
            {
                if (DB.WorkerList[i].Dummy)
                {
                    DB.WorkerList.RemoveAt(i);
                    i--;
                }
            }
            CurrentDummys = 0;
        }
    }
}
