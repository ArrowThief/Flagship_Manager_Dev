using FlagShip_Manager.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace FlagShip_Manager.Management_Server
{
    public class DBObject
    {
        //Custom Database object. 
        //TODO: Repalce with MongoDB or other. 

        public WorkerObject[]? WorkerList { get; set; }
        public Job[]? JobList { get; set; }
        public int[]? ArchiveJobs { get; set; }
        public int[]? ActiveJobs { get; set; }
    }
    public class Database
    {
        //Database class is used for storing and loading DBObjects for long term storge. 

        public static bool Startup = true;
        public static bool UpdateDBFile = false;
        private static readonly string DataBaseFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_DATABASE.txt";
        public static void DataBaseManager()
        {
            //Runs on startup to load existing Database. Then stays in a loop to save database as needed. 
            Load(DataBaseFilePath);
            Thread.Sleep(1000);
            ClearBrokenJobs();
            Thread.Sleep(1000);
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
            //Saves DBObject to disk.
            //TODO: Build DBObject builder.

            DBObject DB = new DBObject();
            DB.JobList = new List<Job>(jobManager.jobList).ToArray();
            DB.ActiveJobs = new List<int>(jobManager.ActiveIDList).ToArray();
            DB.ArchiveJobs = new List<int>(jobManager.ArchiveIDList).ToArray();
            DB.WorkerList = new List<WorkerObject>(WorkerServer.WorkerList).ToArray();
            byte[] DBSerial;
            try
            {

                DBSerial = ObjectToByteArray(DB);
                var Compressed = Misc.CompressArray(DBSerial);
                File.WriteAllBytes(_filePath, Compressed);
            }
            catch (Exception EX)
            {
                Console.WriteLine("Failed to save Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                Console.WriteLine(EX.Message);
            }
        }
        public static void Load(string _filePath)
        {
            //Loads DBObject from disk.

            DBObject DB = new DBObject();
            byte[] DBarry;
            if (File.Exists(_filePath))
            {
                try
                {
                    DBarry = File.ReadAllBytes(_filePath);
                    var Decompress = Misc.DeCompressArray(DBarry);
                    DB = (DBObject)ByteArrayToObject(Decompress);
                }

                catch (Exception EX)
                {
                    Console.WriteLine("Failed to read Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                    Console.WriteLine(EX.Message);
                }
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
            //Checks Loaded database for any Jobs or RenderTasks that were active during shutdown. 
            //If RenderTasks were active, Output files are checked. If files are missing or corrupt A thread is created calling RenderTask.taskFail(). 

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
                        }
                        if (t.Status == 1)
                        {
                            if (j.CheckFiles(t, true))
                            {
                                t.Status = 2;
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
                                Failthreads.Add(new Thread(() => t.taskFail("\n\n JobFailed during server reboot. Moving to new attempt.")));
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
                    foreach (Thread fail in Failthreads)
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
            //Converts Object to byte array. 

            MemoryStream ms = new MemoryStream();
            byte[] Serialized = new byte[0];
            try
            {
                using (BsonWriter bW = new BsonWriter(ms))
                {
                    JsonSerializer ser = new JsonSerializer();
                    ser.Serialize(bW, obj);
                }
                Serialized = ms.ToArray();

            }
            catch (Exception EX)
            {
                Console.WriteLine($"EORROR: {EX}");
            }
            return Serialized;
        }
        public static object? ByteArrayToObject(byte[] arrBytes)
        {
            //Attempts to Converts byte array to DBObject. If fail returns null.

            MemoryStream ms = new MemoryStream(arrBytes);
            var Deserialized = new DBObject();
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
            //Checks for jobs that were duplicated during saving. 
            //TODO: Find the reason Jobs sometimes breake. 

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
