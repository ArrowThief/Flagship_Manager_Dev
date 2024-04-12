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
                SortList.Sort((x, y) => jobManager.JobMap[y].Status = y.CompareTo(jobManager.JobMap[x].Status));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].Status = x.CompareTo(jobManager.JobMap[y].Status));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].Name.CompareTo(jobManager.JobMap[x].Name));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].Name.CompareTo(jobManager.JobMap[y].Name));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].Priority.CompareTo(jobManager.JobMap[x].Priority));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].Priority.CompareTo(jobManager.JobMap[y].Priority));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].Progress.CompareTo(jobManager.JobMap[x].Progress));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].Progress.CompareTo(jobManager.JobMap[y].Progress));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].CreationTime.CompareTo(jobManager.JobMap[x].CreationTime));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].CreationTime.CompareTo(jobManager.JobMap[y].CreationTime));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].ArchiveDate.CompareTo(jobManager.JobMap[x].ArchiveDate));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].ArchiveDate.CompareTo(jobManager.JobMap[y].ArchiveDate));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].RemainingTime.CompareTo(jobManager.JobMap[x].RemainingTime));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].RemainingTime.CompareTo(jobManager.JobMap[y].RemainingTime));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].FrameRange.CompareTo(jobManager.JobMap[x].FrameRange));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].FrameRange.CompareTo(jobManager.JobMap[y].FrameRange));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].RenderApp.CompareTo(jobManager.JobMap[x].RenderApp));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].RenderApp.CompareTo(jobManager.JobMap[y].RenderApp));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].totalActiveTime.CompareTo(jobManager.JobMap[x].totalActiveTime));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].totalActiveTime.CompareTo(jobManager.JobMap[y].totalActiveTime));
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
                SortList.Sort((x, y) => jobManager.JobMap[y].FileFormat.CompareTo(jobManager.JobMap[x].FileFormat));
                LastSort = -1;
            }
            else
            {
                SortList.Sort((x, y) => jobManager.JobMap[x].FileFormat.CompareTo(jobManager.JobMap[y].FileFormat));
                LastSort = 8;
            }
        }
    }
}
