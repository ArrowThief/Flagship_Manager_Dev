namespace FlagShip_Manager.Helpers
{
    public class Sort
    {
        //UI Sorting, currently sorts the actual jobList. 
        //TODO: Create a UI only List so JobManager isn't affected by UI sorting.

        private static int LastSort = 0; //1:Status, 2:Name, 3:Priority, 4: Progress, 5:Oldest, 6:RemainingTime, 7:Lenght, 8:RenderApp, 9:
        private static void Resort()
        {
            if (LastSort == 1) LastSort = -1;
            else LastSort = 1;
        }
        public static void ByStatus(bool Archive, bool resort = false)
        {
            //Sorts by Status

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;
            
            if (LastSort == 1)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).Status.CompareTo(jobManager.jobList.Find(j => j.ID == x).Status));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).Status.CompareTo(jobManager.jobList.Find(j => j.ID == y).Status));
                LastSort = 1;
            }
        }
        public static void ByName(bool Archive, bool resort = false)
        {
            //Sorts by Name

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 2)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).Name.CompareTo(jobManager.jobList.Find(j => j.ID == x).Name));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).Name.CompareTo(jobManager.jobList.Find(j => j.ID == y).Name));
                LastSort = 2;
            }
        }
        public static void ByPriority(bool Archive, bool resort = false)
        {
            //Sorts by Priority

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 3)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).Priority.CompareTo(jobManager.jobList.Find(j => j.ID == x).Priority));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).Priority.CompareTo(jobManager.jobList.Find(j => j.ID == y).Priority));
                LastSort = 3;
            }
        }
        public static void ByProgress(bool Archive, bool resort = false)
        {
            //Sorts by Progress.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;
            if (LastSort == 4)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).Progress.CompareTo(jobManager.jobList.Find(j => j.ID == x).Progress));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).Progress.CompareTo(jobManager.jobList.Find(j => j.ID == y).Progress));
                LastSort = 4;
            }
        }
        public static void ByOldest(bool Archive, bool resort = false)
        {
            //Sorts by age.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;
            if (LastSort == 5)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).CreationTime.CompareTo(jobManager.jobList.Find(j => j.ID == x).CreationTime));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).CreationTime.CompareTo(jobManager.jobList.Find(j => j.ID == y).CreationTime));
                LastSort = 5;
            }
        }
        public static void ByArchevedAge(bool Archive, bool resort = false)
        {
            //Sorts by archive age.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;
            if (LastSort == 5)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).ArchiveDate.CompareTo(jobManager.jobList.Find(j => j.ID == x).ArchiveDate));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).ArchiveDate.CompareTo(jobManager.jobList.Find(j => j.ID == y).ArchiveDate));
                LastSort = 5;
            }
        }
        public static void ByRemaining(bool Archive, bool resort = false)
        {
            //Sorts by remaining time

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 6)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).RemainingTime.CompareTo(jobManager.jobList.Find(j => j.ID == x).RemainingTime));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).RemainingTime.CompareTo(jobManager.jobList.Find(j => j.ID == y).RemainingTime));
                LastSort = 6;
            }
        }
        public static void ByTotalFrames(bool Archive, bool resort = false)
        {
            //Sorts by total frame count.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 7)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).FrameRange.CompareTo(jobManager.jobList.Find(j => j.ID == x).FrameRange));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).FrameRange.CompareTo(jobManager.jobList.Find(j => j.ID == y).FrameRange));
                LastSort = 7;
            }

        }
        public static void ByApp(bool Archive, bool resort = false)
        {
            //Sorts by app.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 8)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).RenderApp.CompareTo(jobManager.jobList.Find(j => j.ID == x).RenderApp));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).RenderApp.CompareTo(jobManager.jobList.Find(j => j.ID == y).RenderApp));
                LastSort = 8;
            }
        }
        public static void ByTimeActive(bool Archive, bool resort = false)
        {
            //Sorts by active time.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 9)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).totalActiveTime.CompareTo(jobManager.jobList.Find(j => j.ID == x).totalActiveTime));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).totalActiveTime.CompareTo(jobManager.jobList.Find(j => j.ID == y).totalActiveTime));
                LastSort = 9;
            }
        }
        public static void ByFormat(bool Archive, bool resort = false)
        {
            //Sorts by output format.

            if (resort) Resort();
            List<int> SortList;
            if (Archive) SortList = jobManager.ArchiveIDList;
            else SortList = jobManager.ActiveIDList;

            if (LastSort == 8)
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == y).FileFormat.CompareTo(jobManager.jobList.Find(j => j.ID == x).FileFormat));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.jobList.Find(j => j.ID == x).FileFormat.CompareTo(jobManager.jobList.Find(j => j.ID == y).FileFormat));
                LastSort = 8;
            }
        }
    }
}
