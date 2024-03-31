using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;

namespace FlagShip_Manager.Helpers
{
    public class WebButtons
    {
        public static void ReturnAndRestartJob(int _ID)
        {
            //var j = jobManager.jobList.Find(job => job.ID == _ID);

            //Job JobtoReturntoQueue = jobManager.jobArchive[JobIndex];
            //JobtoReturntoQueue.Selected = false;
            //RestartJob(JobtoReturntoQueue);
            //jobManager.jobList.Add(JobtoReturntoQueue);

            jobManager.ActiveIDList.Add(_ID);
            jobManager.ArchiveIDList.Remove(_ID);
            RestartJob(_ID);
            //jobManager.jobArchive.RemoveAt(JobIndex);
            //Database.Save(Database.DataBaseFile);

        }
        public static void ReturnJobtoQueue(int _ID)
        {
            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            //Job j = jobManager.jobArchive[JobIndex];
            //j.Selected = false;
            //j.StopUpdate = false;
            j.Archive = false;
            j.ArchiveDate = DateTime.MaxValue;
            if (!j.finished)
            {
                j.Status = 0;
                foreach (var rt in j.renderTasks)
                {
                    if (rt.Status == 2) continue;
                    rt.Status = 0;
                }
            }
            //jobManager.jobList.Add(j);
            if(!jobManager.ActiveIDList.Contains(_ID))jobManager.ActiveIDList.Add(_ID);
            jobManager.ArchiveIDList.Remove(_ID);
            //jobManager.jobArchive.RemoveAt(JobIndex);
            //Database.Save(Database.DataBaseFile);
        }
        public static void ResumeJob(int _ID)
        {
            var JobIndex = jobManager.jobList.FindIndex(job => job.ID == _ID);
            Job j = jobManager.jobList[JobIndex];
            j.selected = false;
            //if (j.Status != 3 || j.Status != 5) return;
            //j.StopUpdate = false;
            foreach (var rt in j.renderTasks)
            {
                if (rt.Status == 2) continue;
                rt.Status = 0;
            }
            j.Status = 0;
            j.PauseRequest = false;
            //Database.Save(Database.DataBaseFile);
        }
        public static void PauseJob(int _ID)
        {

            var JobIndex = jobManager.jobList.FindIndex(job => job.ID == _ID);
            Job j = jobManager.jobList[JobIndex];
            j.selected = false;
            if (j.Status == 0 || j.Status == 1)
            {
                //j.StopUpdate = true;
                foreach (var rt in j.renderTasks)
                {
                    //int LogIndex = rt.Log.Count - 1;
                    if (rt.Status == 1)
                    {

                        try
                        {
                            rt.taskLogs.WriteToWorker($"\n-------------------------------Worker Log end-------------------------------\nJob Paused. {rt.taskLogs.CurrentWorker()} set cancel request.");
                            //WorkerServer.cancelWorker(WorkerServer.WorkerList[Logic.matchTasktoWorker(rt)], false, false);
                            WorkerServer.cancelWorker(rt.Worker(), false, false);
                        }
                        catch (Exception e)
                        {
                            rt.taskLogs.WriteToWorker("Unable to cancel worker.");
                            Console.WriteLine(e);
                        }
                        rt.ExistingFrames = rt.FinishedFrameNumbers.Count();
                        //Logic.taskFail(rt.ID, j.ID, rt.taskLogs.WorkerIDs.Last(), "Job Paused.");
                        rt.taskFail("Job Paused");
                        //rt. ++;
                        rt.ExistingProgress = rt.progress;
                    }
                    else if (rt.Status == 2 || rt.progress > 99) continue;
                    //rt.Status = 3;
                }
                j.PauseRequest = true;
                j.Status = 3;
                //Database.Save(Database.DataBaseFile);
            }
            else return;
        }
        public static void CancelJob(int _ID)
        {
            var JobIndex = jobManager.jobList.FindIndex(job => job.ID == _ID);
            Job j = jobManager.jobList[JobIndex];
            j.selected = false;
            //j.StopUpdate = true;
            foreach (var rt in j.renderTasks)
            {
                if (rt.FinishReported) continue;
                if (rt.Status == 1)rt.taskFail("Render Cancel Requested.", true); // Logic.taskFail(_ID, rt.ID, rt.taskLogs.WorkerIDs.Last(), "Render Cancel Requested.", true, false);
                else if (rt.Status == 2) continue;
                rt.Status = 5;
                //rt.finished = false;

                /*for (int i = 0; i < 5; i++)
                {
                    rt.Worker[i] = "";
                }*/
            }
            j.EndTimes.Add(DateTime.Now);
            j.Status = 5;
            //Database.Save(Database.DataBaseFile);
        }
        public static void RestartJob(int _ID)
        {
            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            DateTime start = DateTime.Now;
            j.SetOutputOffset();
            foreach (var rt in j.renderTasks)
            {
                if (rt.Status == 1) WorkerServer.cancelWorker(WorkerServer.WorkerList.Find(w => w.WorkerID == rt.taskLogs.WorkerIDs.Last()), false, false);
                rt.reset();
            }
            //j.ShotAlert = false;
            j.Archive = false;
            j.finished = false;
            j.fail = false;
            j.ShotAlert = false;
            j.CompletedFrames = 0;

            j.Status = 0;
            j.CreationTime = start;
            j.Progress = 0;
            j.MachineHours = TimeSpan.Zero;
            j.RemainingTime = TimeSpan.Zero;
            //j.RenderFinishTime = start;
            j.totalActiveTime = TimeSpan.Zero;
            j.StartTimes.Clear();
            j.EndTimes.Clear();
            j.ArchiveDate = DateTime.MaxValue;
            j.TimePerFrame = 0;
        }
        public static void RestartTask(int _JID, int _TID)
        {
            Job j = jobManager.jobList[jobManager.jobList.FindIndex(job => job.ID == _JID)];
            j.selected = false;
            renderTask rT = j.renderTasks.Find(t => t.ID == _TID);

            float ProgressPerTask = 100 / j.renderTasks.Count();
            if (rT.progress > 0 && rT.Status != 2) j.Progress -= (ProgressPerTask / 100) * rT.progress;
            else if (rT.Status == 2) j.Progress -= (float)Math.Round(ProgressPerTask);
            rT.reset();
            j.finished = false;
            //j.StopUpdate = false;
            if (j.Status != 1) j.Status = 0;
            if (j.Archive)
            {
                jobManager.ArchiveIDList.Remove(j.ID);
                jobManager.ActiveIDList.Add(j.ID);
                j.Archive = false;
                j.ArchiveDate = DateTime.MaxValue;
            }

        }
        public static void RemoveJob(int _ID)
        {
            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            //Job j = jobManager.jobArchive[JobIndex];
            jobManager.ArchiveIDList.Remove(_ID);
            if (File.Exists(j.Project))
            {
                try
                {
                    File.Delete(j.Project);
                    //Console.WriteLine($"Pretending to delete {j.Project}");
                }
                catch
                {
                    Console.WriteLine("Cannot remove project file. it will remain on server.");
                }
            }
            jobManager.jobList.Remove(j);
            //Database.Save(Database.DataBaseFile);
        }
        public static void ArchiveJob(int _ID)
        {
            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            //Job JobtoArchive = jobManager.jobList[JobIndex];
            if (j.Status == 0 || j.Status == 1 || j.Status == 3) CancelJob(_ID); //Job/Task status. queued(0), Rendering(1), finished(2) paused(3), fialed(4), canceled(5).
            j.Archive = true;
            j.ArchiveDate = DateTime.Now;
            jobManager.ActiveIDList.Remove(_ID);
            if(!jobManager.ArchiveIDList.Contains(_ID))jobManager.ArchiveIDList.Add(_ID);

            //JobtoArchive.Selected = false;
            //jobManager.jobArchive.Add(JobtoArchive);
            //jobManager.jobList.RemoveAt(JobIndex);
            /*try {
                Database.Save(Program.DataBaseFile);
            }
            catch(Exception EX)
            {
                Console.WriteLine("ERROR: " + EX);
            }*/
            //Console.WriteLine("TEST");
        }
        
       
        public static void RunAction(int Command, int[] Selected)//1 Cancel, 2:Pause, 3:Restart, 4:Return to Queue and restart, 5:Archive, 6:Resume, 7:Remove 8:Return to Queue
        {
            foreach (int JID in Selected)
            {
                switch (Command)
                {

                    case (1):
                        CancelJob(JID);
                        break;
                    case (2):
                        PauseJob(JID);
                        break;
                    case (3):
                        RestartJob(JID);
                        break;
                    case (4):
                        ReturnAndRestartJob(JID);
                        break;
                    case (5):
                        ArchiveJob(JID);
                        break;
                    case (6):
                        ResumeJob(JID);
                        break;
                    case (7):
                        RemoveJob(JID);
                        break;
                    case (8):
                        ReturnJobtoQueue(JID);
                        break;
                }
            }
            Database.UpdateDBFile = true;
        }
    }
}
