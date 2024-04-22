using FlagShip_Manager.Management_Server;

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

        }
        public static void ByName(bool Archive, bool resort = false)
        {
            //Sorts by Name

            
        }
        public static void ByPriority(bool Archive, bool resort = false)
        {
            //Sorts by Priority

        }
        public static void ByProgress(bool Archive, bool resort = false)
        {
            //Sorts by Progress.

        }
        public static void ByOldest(bool Archive, bool resort = false)
        {
            //Sorts by age.

        }
        public static void ByArchevedAge(bool Archive, bool resort = false)
        {
            //Sorts by archive age.

        }
        public static void ByRemaining(bool Archive, bool resort = false)
        {
            //Sorts by remaining time
;
            
        }
        public static void ByTotalFrames(bool Archive, bool resort = false)
        {
            //Sorts by total frame count.

         

        }
        public static void ByApp(bool Archive, bool resort = false)
        {
            //Sorts by app.

        }
        public static void ByTimeActive(bool Archive, bool resort = false)
        {
            //Sorts by active time.

        }
        public static void ByFormat(bool Archive, bool resort = false)
        {
            //Sorts by output format.

        }
    }
}
