using FlagShip_Manager.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;

namespace FlagShip_Manager.Management_Server
{
    public class DBObject
    {
        //Custom Database object. 
        //TODO: Repalce with MongoDB or other. 

        public Worker[]? WorkerList { get; set; }
        public Job[]? ArchiveJobs { get; set; }
        public Job[]? ActiveJobs { get; set; }

        public DBObject(Job[]? _active = null, Job[]? _archive = null, Worker[]? _worker = null) 
        {
            if(_active == null)ActiveJobs = new Job[0];
            else ActiveJobs = _active;
            if(_archive == null)ArchiveJobs = new Job[0];
            else ArchiveJobs = _archive;
            if(_worker == null)WorkerList = new Worker[0];      
            else WorkerList = _worker;
        }
    }
    public class DB
    {
        //Database class is used for storing and loading DBObjects for long term storge. 

        public static bool Startup = true;
        public static bool UpdateDBFile = false;
        private static readonly string DataBaseFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_DATABASE.txt";
        
        public static List<Job> active = new List<Job>();
        public static List<int> removeActive = new List<int>();
        private static int activeID = 0;

        public static List<Job> archive = new List<Job>();
        public static List<int> removeArchive = new List<int>();
        private static int archiveID = 0;

        public static List<Worker> workers = new List<Worker>();
        public static List<int> removeWorker = new List<int>();
        private static int workerID = 0;

        public static void DataBaseManager()
        {
            //Runs on startup to load existing Database. Then stays in a loop to save database as needed. 
            
            Load(DataBaseFilePath);

            while (true)
            {
                if (UpdateDBFile) Save(DataBaseFilePath);
                Thread.Sleep(60000);
            }
        }
        public static void Save(string _filePath)
        {
            //Saves DBObject to disk.

            byte[] DBSerial;
            try
            {
                DBSerial = ObjectToByteArray(new DBObject(active.ToArray(), archive.ToArray(), DB.workers.ToArray()));
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

            DBObject newDB = new DBObject();
            byte[] DBarry;
            if (File.Exists(_filePath))
            {
                try
                {
                    DBarry = File.ReadAllBytes(_filePath);
                    var Decompress = Misc.DeCompressArray(DBarry);
                    newDB = (DBObject)ByteArrayToObject(Decompress);
                }

                catch (Exception EX)
                {
                    Console.WriteLine("Failed to read Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                    Console.WriteLine(EX.Message);
                }
                if (newDB != null)
                {
                    CheckDatabase(newDB);
                    foreach (var worker in workers)
                    {
                        worker.Status = 7;
                        workers.Add(worker);
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
            if (_DB.ActiveJobs != null)
            {
                foreach (Job j in _DB.ActiveJobs)
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
                    if (j.renderTasks.Length == finishCount)
                    {
                        j.Status = 2;
                        j.finished = true;
                        j.Progress = 100;
                        j.CompletedFrames = j.TotalFramesToRender;
                    }
                    else if (j.Status == 1) j.Status = 0;
                    active.Add(j);
                    foreach (Thread fail in Failthreads)
                    {
                        fail.Start();
                        Thread.Sleep(10);
                    }
                }
            }
            if (_DB.ActiveJobs != null) active = _DB.ActiveJobs.ToList();
            if (_DB.ArchiveJobs != null) active = _DB.ArchiveJobs.ToList();


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
        public static int NextActive()
        {
            return activeID++;
        }
        public static int NextArchive()
        {
            return activeID++;
        }
        public static int NextWorker(bool peek = false)
        {
            if (peek) return workerID;
            else return workerID++;
        }
        internal static Job? FindJob(List<Job> searchList, int target)
        {
            //Binary search for Job in List.

            int mid = searchList.Count() / 2;
            if (searchList[mid].ID == target) return searchList[mid];
            else if (searchList[mid].ID > target)
            {
                return FindJob(searchList.GetRange(0, mid-1), target);
            }
            else if (searchList[mid].ID < target)
            {
                return FindJob(searchList.GetRange(mid+1, searchList.Count()-mid-1), target);
            }
            return null;
        }
        internal static Worker? FindWorker(List<Worker> searchList, int target)
        {
            //Binary search through Archive Job list.

            int mid = searchList.Count() / 2;
            if (searchList[mid].ID == target) return searchList[mid];
            else if (searchList[mid].ID > target)
            {
                return FindWorker(searchList.GetRange(0, mid - 1), target);
            }
            else if (searchList[mid].ID < target)
            {
                return FindWorker(searchList.GetRange(mid + 1, searchList.Count() - mid - 1), target);
            }
            return null;
        }

    }

}
