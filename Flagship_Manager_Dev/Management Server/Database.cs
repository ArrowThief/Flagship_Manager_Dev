using FlagShip_Manager.Objects;
//using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace FlagShip_Manager.Management_Server
{
    public class DBObject
    {
        public WorkerObject[]? WorkerList { get; set; }
        public Job[]? JobList { get; set; }
        public int[]? ArchiveJobs { get; set; }
        public int[]? ActiveJobs { get; set; }
    }
    public class Database
    {
        public static bool Startup = true;
        public static bool UpdateDBFile = false;
        //private static DateTime LastSave = DateTime.MinValue;
        private static readonly string DataBaseFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_DATABASE.txt";
        //public static string SettingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_Server_Settings.txt";
        public static void DataBaseManager()
        {
            Load(DataBaseFilePath);

            Thread.Sleep(4000);
            ClearBrokenJobs();
            Thread.Sleep(1000);
            //Startup = false;
            while (true)
            {
                if (UpdateDBFile)
                {
                    Save(DataBaseFilePath);
                    Thread.Sleep(60000);
                }
                else Thread.Sleep(1000);
            }
        }
        public static void Save(string _filePath)
        {

            DBObject DB = new DBObject();
            //DB.WorkerList = WorkerServer.WorkerList.ToArray();
            //List<Job> TempList = new List<Job>();
            DB.JobList = new List<Job>(jobManager.jobList).ToArray();
            //            foreach (var job in DB.JobList)
            //            {
            //                job.ProgressThread = null;
            //            }
            DB.ActiveJobs = new List<int>(jobManager.ActiveIDList).ToArray();
            DB.ArchiveJobs = new List<int>(jobManager.ArchiveIDList).ToArray();
            //DB.ArchiveJobList = jobManager.jobArchive.ToArray();
            DB.WorkerList = new List<WorkerObject>(WorkerServer.WorkerList).ToArray();
            byte[] DBSerial;
            try
            {

                DBSerial = ObjectToByteArray(DB);
                var Compressed = Misc.CompressArray(DBSerial);
                File.WriteAllBytes(_filePath, Compressed);
                //LastSave = DateTime.Now;
            }
            catch (Exception EX)
            {
                Console.WriteLine("Failed to save Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                Console.WriteLine(EX.Message);
            }
        }
        public static void Load(string _filePath)
        {
            DBObject DB = new DBObject();
            byte[] DBarry;
            if (File.Exists(_filePath))
            {
                try
                {
                    DBarry = File.ReadAllBytes(_filePath);
                    var Decompress = Misc.DeCompressArray(DBarry);
                    DB = ByteArrayToObject(Decompress);
                }

                catch (Exception EX)
                {
                    Console.WriteLine("Failed to read Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                    Console.WriteLine(EX.Message);
                }
                //DB = ByteArrayToObject(DBString.Split());
                if (DB != null)
                {
                    CheckDatabase(DB);
                    foreach (var worker in DB.WorkerList)
                    {
                        worker.Status = 7;
                        WorkerServer.WorkerList.Add(worker);
                    }
                }
            }
            Startup = false;
        }
        public static void CheckDatabase(DBObject _DB)
        {
            int finishCount;
            if (_DB == null) return;
            if (_DB.JobList != null)
            {
                foreach (Job j in _DB.JobList)
                {
                    
                    finishCount = 0;
                    List<Thread> Failthreads = new List<Thread>();
                    foreach (renderTask t in j.renderTasks)
                    {
                        int LogIndex = t.taskLogs.Attempt();
                        if (LogIndex > 4)
                        {
                            t.Status = 4;
                            j.Status = 4;
                            //t.attempt = 4;
                        }
                        //int BlankCount = 0;
                        //for (int i = 0; i < LogIndex; i++) if (t.Log[i] == "") BlankCount++;
                        if (t.Status == 1)
                        {
                            /*
                            for (int i = 0; i < t.Log.Count(); i++)
                            {
                                if (t.Log[i] == "" && BlankCount > 1)
                                {
                                    t.Log.RemoveAt(LogIndex);
                                    LogIndex--;
                                    i--;
                                }
                            }
                            */
                            if (j.CheckFiles(t, true))
                            {
                                t.Status = 2;
                                //t.finished = true;
                                if (t.taskLogs.CurrentWorker == null && LogIndex > 0)
                                {
                                    t.taskLogs.removeLast();
                                    LogIndex--;
                                }
                                if (t.finishTime == DateTime.MinValue) t.finishTime = DateTime.Now;
                                continue;
                            }
                            if (t.taskLogs.WorkerLog[LogIndex] != "" && t.progress != 100)
                            {
                                Failthreads.Add(new Thread(() => t.taskFail("\n\n JobFailed during server reboot. Moving to new attempt."))); //Logic.taskFail(j.ID, t.ID, t.taskLogs.WorkerIDs.Last(), "\n\n JobFailed during server reboot. Moving to new attempt.")));
                            }
                            t.Status = 0;

                        }
                        else if (t.Status == 2)
                        {
                            finishCount++;
                        }
                    }
                    if (j.renderTasks.Count == finishCount)
                    {
                        j.Status = 2;
                        j.finished = true;
                        j.Progress = 100;
                        j.CompletedFrames = j.TotalFramesToRender;
                    }
                    else if (j.Status == 1) j.Status = 0;
                    jobManager.jobList.Add(j);
                    foreach(Thread fail in Failthreads)
                    {
                        fail.Start();
                        Thread.Sleep(10);
                    }
                }
            }
            if (_DB.ActiveJobs != null) jobManager.ActiveIDList = _DB.ActiveJobs.ToList();
            if (_DB.ArchiveJobs != null) jobManager.ArchiveIDList = _DB.ArchiveJobs.ToList();


        }
        public static byte[] ObjectToByteArray(Object obj)
        {
            MemoryStream ms = new MemoryStream();
            byte[] Serialized = new byte[0];
            try
            {
                using (BsonWriter bW = new BsonWriter(ms))
                {
                    JsonSerializer ser = new JsonSerializer();
                    ser.Serialize(bW, obj); //This is throwing an error.
                }
                Serialized = ms.ToArray();

            }
            catch (Exception EX)
            {
                Console.WriteLine($"EORROR: {EX}");
            }
            return Serialized;
        }
        public static DBObject? ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream ms = new MemoryStream(arrBytes);
            DBObject Deserialized = new DBObject();
            try
            {
                using (BsonReader bR = new BsonReader(ms))
                {
                    JsonSerializer ser = new JsonSerializer();
                    Deserialized = ser.Deserialize<DBObject>(bR);
                }

            }
            catch (Exception EX)
            {
                Console.WriteLine($"EORROR: {EX}");
            }
            return Deserialized;
        }
        private static void ClearBrokenJobs()
        {
            int count = 0;
            List<int> RemoveList = new List<int>();
            Thread.Sleep(1000);
            for (int i = 0; i < jobManager.jobList.Count; i++)
            {
                if (!jobManager.ActiveIDList.Contains(jobManager.jobList[i].ID))
                {
                    if (!jobManager.ArchiveIDList.Contains(jobManager.jobList[i].ID)) RemoveList.Add(i);
                }
            }
            foreach (int i in RemoveList)
            {
                jobManager.jobList.RemoveAt(i - count);
                count++;
            }
            Thread.Sleep(100);
        }

    }

}
