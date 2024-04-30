using FlagShip_Manager.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;

namespace FlagShip_Manager.Management_Server
{
    public class DBObject
    {
        //Database Objects or DBO is used for saving and loading the database on reboot. 

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
        //On server reboot all IDs will be reset to 100 in an effort to never run out of ID numbers.
        //Worker will count up to the ID of how many workers are in the Database while preserving the existing worker IDs. 

        public static bool Startup = true;
        public static bool UpdateDBFile = false;
        private static readonly string DataBaseFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_DATABASE.txt";
        
        public static List<Job> active = new List<Job>();
        private static int activeID = 100;

        public static List<Job> archive = new List<Job>();
        private static int archiveID = 100;

        public static List<Worker> workers = new List<Worker>();
        private static int workerID = 100;

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
                    foreach (var worker in newDB.WorkerList)
                    {
                        worker.Status = 7;
                        workers.Add(worker);
                        workerID++;
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
            if (_DB == null || _DB.ActiveJobs == null) return;

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
                            t.taskFail("\n\n JobFailed during server reboot. Moving to new attempt.");
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
                AddToActive(j);
            }
            foreach(Job j in _DB.ArchiveJobs)
            {
                AddToArchive(j,false);
            }

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
            //Returns the next activeID and increeses the ID by 1.
            return activeID++;
        }
        public static int NextArchive()
        {
            //Returns the next archiveID and increeses the ID by 1.
            return archiveID++;
        }
        public static int NextWorker(bool peek = false)
        {
            //Returns the next workerID and increeses the ID by 1.
            //Allso allows for peeking at current ID.

            if (peek) return workerID;
            else return workerID++;
        }
        public static void AddToActive(Job addJob, bool fromArchive = false)
        {
            //Gives Job new active list ID and adds to active list. 
            //If fromArchive is true, job is found in archive and removed before being added to active. 
            
            if (fromArchive)
            {
                int index = FindJob(archive, addJob.ID);
                archive.RemoveAt(index);
                addJob.ArchiveDate = DateTime.MaxValue;
                addJob.Archive = false; 
            }
            addJob.AssignNewID(NextActive());
            active.Add(addJob);
            
        }
        public static void AddToArchive(Job addJob, bool fromActive)
        {
            //Removes job from active list, assigns a new archive ID and archive Date,then adds to archive list. 

            
            if(fromActive)
            {
                if (addJob.Status == 0 || addJob.Status == 1 || addJob.Status == 3) addJob.Cancel();
                addJob.ArchiveDate = DateTime.Now;
                addJob.Archive = true;
                int index = FindJob(active, addJob.ID);
                active.RemoveAt(index);
            }
            addJob.AssignNewID(NextArchive());
            archive.Add(addJob);

        }
        public static void RemoveJob(Job remove)
        {
            //Removes Job from Database. 

            int index = FindJob(archive, remove.ID);
            archive.RemoveAt(index);
            try
            {
                File.Delete(remove.Project);
                File.Delete($"{jobManager.ActiveSettings.CtlFolder}\\Archive\\{remove.Name}.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to remove project file. \n"+ex.ToString());
            }
        }
        internal static int FindJob(List<Job> searchList, int target)
        {
            //Binary search for Job in List, returns index of job.

            int min = 0;
            int max = searchList.Count()-1;
            int mid = (min+max) / 2;
            while (min <= max)
            {
                if(target == searchList[mid].ID) return mid;
                else if(target < searchList[mid].ID)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
                mid = (min + max) / 2;
            }
            return -1;
            }
        
        internal static Worker? FindWorker(int target)
        {
            //Binary search through Archive Job list.

            int min = 0;
            int max = workers.Count() - 1;
            int mid = (min + max) / 2;
            while (min <= max)
            {
                if (target == workers[mid].ID) return workers[mid];
                else if (target < workers[mid].ID)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
                mid = (min + max) / 2;
            }
            return null;
        }
    }
}
