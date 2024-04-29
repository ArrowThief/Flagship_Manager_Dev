using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;

namespace FlagShip_Manager.Helpers
{
    public class WebButtons
    {
        //UI buttons class.

        public static void ReturnAndRestartJob(Job j)
        {
            //Moves job from Archive to Active queue and restarts it.

            RestartJob(j);
            DB.AddToActive(j, true);
        }
        public static void ReturnJobToQueue(Job j)
        {
            //Moves job from Archive to Active queue.
            
            if (!j.finished)
            {
                j.Status = 0;
                foreach (var rt in j.renderTasks)
                {
                    if (rt.Status != 2) rt.Status = 0;
                }
            }
            DB.AddToActive(j, true);
        }
        public static void PauseJob(Job j)
        {
            //Pauses Job. Also cancels any active tasks. 

            j.PauseOrResume();
        }
        public static void CancelJob(Job j)
        {
            //Cancels job. If any renderTasks are active cancel requests are sent to workers.

            j.Cancel();
        }
        public static void RestartJob(Job j)
        {
            //Restarts job from begining. If any renderTasks are active cancel requests are sent to workers. 
            
            j.Restart();
        }
        public static void RestartTask(Job j, int _rI)
        {
            //Restarts a single renderTask, removes progress from Job.
            //If Job is in archive queue, it is returned to active queue.

            j.renderTasks[_rI].Restart(j.Archive, true);
        }
        public static void RemoveJob(Job j)
        {
            //Removes job from Archive queue. 
            //Also deletes Project file from storage.
            
            DB.RemoveJob(j);

        }
        public static void ArchiveJob(Job j)
        {
        
            //Moves job from Active Queue to Archive. 
            //If any renderTasks are active, cancel requests are sent to workers.
           
            DB.AddToArchive(j, true); 
        }
        
       
        public static void RunAction(int Command, Job[] Selected)
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


            foreach (Job j in Selected)
            {
                switch (Command)
                {

                    case (1):
                        CancelJob(j);
                        break;
                    case (2):
                        PauseJob(j);
                        break;
                    case (3):
                        RestartJob(j);
                        break;
                    case (4):
                        ReturnAndRestartJob(j);
                        break;
                    case (5):
                        ArchiveJob(j);
                        break;
                    case (6):
                        PauseJob(j);
                        break;
                    case (7):
                        RemoveJob(j);
                        break;
                    case (8):
                        ReturnJobToQueue(j);
                        break;
                }
            }
            DB.UpdateDBFile = true;
        }
    }
}
