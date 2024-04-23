using FlagShip_Manager.Management_Server;
using FlagShip_Manager.Objects;
using System.Text.RegularExpressions;

namespace FlagShip_Manager
{
    internal class Logic
    {
        //A class for storing logical operations. 
        //TODO: Move methods into thier proper classes. 

        public static List<Job> getQueuedJobs(IList<Job> jobList)
        {
            //Gets a list of jobs that haven't yet been rendered. 

            //Job/Task status Map:
            //0 = queued.
            //1 = Rendering.
            //2 = complete.
            //3 = paused.
            //4 = fialed.
            //5 = canceled.

            List<Job> _returnList = new List<Job>();
            for (int i = 0; i < jobList.Count(); i++)
            {
                Job j = jobList[i];
                if (j.finished) continue;
                else if (j.fail)
                {
                    if (j.Status != 4)
                    {
                        j.Status = 4;

                        foreach (var rt in j.renderTasks)
                        {
                            if (rt.Status == 2 || rt.Status == 4) continue;
                            else if (rt.Status == 0 || rt.Status == 1) rt.Status = 5;
                            else rt.Status = 4;
                        }
                    }
                }
                if (j.Status == 0 || j.Status == 1) { 
                
                    _returnList.Add(j);
                }
            }
            if (_returnList.Count > 0) _returnList.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return _returnList;
        }
        internal static List<int> getAvailableWorkers(Job _job)
        {
            //returns a list of avalable workers indecies. 
            //TODO: Rewrite everything, move class.

            List<int> _return = new List<int>();
            var workers = DB.WorkerList;
            var activeJobs = new List<Job>();
            for(int jindex = 0; jindex < DB.active.Count(); jindex++)
            {
                Job j = DB.active[jindex];
                if (j.Status == 0 || j.Status == 1) activeJobs.Add(j);//Builds a list of jobs that are currently waiting to be rendered.            
            }
            for (int wI = 0; wI < workers.Count(); wI++)
            {
                Worker worker = workers[wI];
                RenderApp Default = worker.AvailableApps.Find(a => a.Default == true); //Check if the worker has a default render type.
                RenderApp CurrentJob = worker.AvailableApps.Find(a => a.AppName == _job.RenderApp);
                if (CurrentJob == null || worker.Dummy) continue;//Check if Worker can render this Job type.
                else if (!CurrentJob.Enabled) continue;
                //if (worker.Dummy) continue;
                if (worker.awaitUpdate || worker.lastSubmittion.AddSeconds(5) > DateTime.Now) continue;
                if (_job.WorkerBlackList.Contains(worker.ID)) continue;//Checks the jobs blacklist for worker.
                if (_job.GPU && !worker.GPU) continue;

                if (worker.Status == 0)//Client is ready to work.
                {
                    if (Default != null)
                    {
                        if (Default.AppName != _job.RenderApp && Default.Enabled)//Checks if the worker's default render app is the same as the Jobs.
                        {
                            if (activeJobs.Any(j => j.RenderApp == Default.AppName)) continue; //If the worker is able to render this type but it isn't the default, checks if there is a default Job in the queue.   
                        }
                    }
                    _return.Add(wI);//If one or more of the previous checks pass then worker is added to list of available workers for the job.
                }

            }

            return _return;
        }
        public static string GetRenderType(string _outputType, string _outputPath)
        {
            //retuns a string with the file extention of the output file. 
            //differentiates between .mov 4444 and 422 for better user info.

            var Type = (_outputType);
            var FileType = Path.GetExtension(_outputPath).Substring(1);
            if (FileType.ToLower() == "mov")
            {
                if (Regex.IsMatch(Type, "(4444)"))
                {
                    return "4444";
                }
                else if (Regex.IsMatch(Type, "(422)"))
                {
                    return "422";
                }
            }
            return FileType.ToUpper();
        }

        public static void BuildFolderPath(string dir) 
        {
            //Checks if Dir tree exists, if false then tree is built. 

            if (!Directory.Exists(dir))
            {
                BuildFolderPath(Directory.GetParent(dir).FullName);
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Failed to make folder at: " + dir);
                }
                
            }
        }
    }
}