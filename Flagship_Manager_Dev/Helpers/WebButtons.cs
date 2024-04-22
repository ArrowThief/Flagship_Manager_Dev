using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;

namespace FlagShip_Manager.Helpers
{
    public class WebButtons
    {
        //UI buttons class.

        public static void ReturnAndRestartJob(int _ID)
        {
            //Moves job from Archive to Active queue and restarts it.

            jobManager.ActiveIDList.Add(_ID);
            jobManager.ArchiveIDList.Remove(_ID);
            RestartJob(_ID);

        }
        public static void ReturnJobtoQueue(int _ID)
        {
            //Moves job from Archive to Active queue.

            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
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
            if(!jobManager.ActiveIDList.Contains(_ID))jobManager.ActiveIDList.Add(_ID);
            jobManager.ArchiveIDList.Remove(_ID);
        }
        public static void ResumeJob(int _ID)
        {
            //Resumes a paused job.

            var JobIndex = jobManager.jobList.FindIndex(job => job.ID == _ID);
            Job j = jobManager.jobList[JobIndex];
            j.selected = false;
            foreach (var rt in j.renderTasks)
            {
                if (rt.Status == 2) continue;
                rt.Status = 0;
            }
            j.Status = 0;
            j.PauseRequest = false;
        }
        public static void PauseJob(int _ID)
        {
            //Pauses Job. Also cancels any active tasks. 

            var JobIndex = jobManager.jobList.FindIndex(job => job.ID == _ID);
            Job j = jobManager.jobList[JobIndex];
            j.selected = false;
            if (j.Status == 0 || j.Status == 1)
            {
                foreach (var rt in j.renderTasks)
                {
                    if (rt.Status == 1)
                    {
                        try
                        {
                            rt.taskLogs.WriteToWorker($"\n-------------------------------Worker Log end-------------------------------\nJob Paused. {rt.taskLogs.CurrentWorker()} set cancel request.");
                            WorkerServer.cancelWorker(rt.Worker(), false, false);
                        }
                        catch (Exception e)
                        {
                            rt.taskLogs.WriteToWorker("Unable to cancel worker.");
                            Console.WriteLine(e);
                        }
                        rt.ExistingFrames = rt.FinishedFrameNumbers.Count();
                        rt.taskFail("Job Paused");
                        rt.ExistingProgress = rt.progress;
                    }
                    else if (rt.Status == 2 || rt.progress > 99) continue;
                }
                j.PauseRequest = true;
                j.Status = 3;
            }
            else return;
        }
        public static void CancelJob(int _ID)
        {
            //Cancels job. If any renderTasks are active cancel requests are sent to workers.

            var JobIndex = jobManager.jobList.FindIndex(job => job.ID == _ID);
            Job j = jobManager.jobList[JobIndex];
            j.selected = false;
            foreach (var rt in j.renderTasks)
            {
                if (rt.FinishReported) continue;
                if (rt.Status == 1)rt.taskFail("Render Cancel Requested.", true);
                else if (rt.Status == 2) continue;
                rt.Status = 5;
            }
            j.EndTimes.Add(DateTime.Now);
            j.Status = 5;
        }
        public static void RestartJob(int _ID)
        {
            //Restarts job from begining. If any renderTasks are active cancel requests are sent to workers. 
            //TODO: Most of this should become a class method.

            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            DateTime start = DateTime.Now;
            j.SetOutputOffset();
            foreach (var rt in j.renderTasks)
            {
                if (rt.Status == 1) WorkerServer.cancelWorker(WorkerServer.WorkerList.Find(w => w.WorkerID == rt.taskLogs.WorkerIDs.Last()), false, false);
                rt.reset();
            }
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
            j.totalActiveTime = TimeSpan.Zero;
            j.StartTimes.Clear();
            j.EndTimes.Clear();
            j.ArchiveDate = DateTime.MaxValue;
            j.TimePerFrame = 0;
        }
        public static void RestartTask(int _JID, int _TID)
        {
            //Restarts a single renderTask, removes progress from Job.
            //If Job is in archive queue, it is returned to active queue.

            Job j = jobManager.jobList[jobManager.jobList.FindIndex(job => job.ID == _JID)];
            j.selected = false;
            renderTask rT = j.renderTasks[_TID];

            float ProgressPerTask = 100 / j.renderTasks.Count();
            if (rT.progress > 0 && rT.Status != 2) j.Progress -= (ProgressPerTask / 100) * rT.progress;
            else if (rT.Status == 2) j.Progress -= (float)Math.Round(ProgressPerTask);
            rT.reset();
            j.finished = false;
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
            //Removes job from Archive queue. 
            //Also deletes Project file from storage.

            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            jobManager.ArchiveIDList.Remove(_ID);
            if (File.Exists(j.Project))
            {
                try
                {
                    File.Delete(j.Project);
                }
                catch
                {
                    Console.WriteLine("Cannot remove project file. it will remain on server.");
                }
            }
            jobManager.jobList.Remove(j);
        }
        public static void ArchiveJob(int _ID)
        {
        
            //Moves job from Active Queue to Archive. 
            //If any renderTasks are active, cancel requests are sent to workers.
            
            var j = jobManager.jobList.Find(job => job.ID == _ID);
            j.selected = false;
            if (j.Status == 0 || j.Status == 1 || j.Status == 3) CancelJob(_ID);
            j.Archive = true;
            j.ArchiveDate = DateTime.Now;
            jobManager.ActiveIDList.Remove(_ID);
            if(!jobManager.ArchiveIDList.Contains(_ID))jobManager.ArchiveIDList.Add(_ID);
            
        }
        
       
        public static void RunAction(int Command, int[] Selected)
        {
            //Method simplifies action calling for button presses.

            // Case Map:
            // 1 Cancel,
            // 2:Pause,
            // 3:Restart,
            // 4:Return to Queue and restart,
            // 5:Archive,
            // 6:Resume,
            // 7:Remove
            // 8:Return to Queue


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
            DB.UpdateDBFile = true;
        }
    }
}
