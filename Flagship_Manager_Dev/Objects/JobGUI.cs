namespace FlagShip_Manager.Objects
{
    public class JobGUI
    {
        public int ID { get; set; }
        public bool extend { get; set; } = false;
        public string JobArrow { get; set; } = "Images/arrow right.png";
        public string TaskArrow { get; set; } = "Images/arrow down.png";
        public bool Open { get; set; } = false;
        public string BlackListClass { get; set; } = "ScrollList";
        public string TaskListClass { get; set; } = "TaskList";
        public List<int> CheckedIDs { get; set; } = new List<int>();
    }
}
