namespace FlagShip_Manager.Objects
{
    public class importRender
    {
        public string? Project { get; set; }
        public string? WorkingProject { get; set; }
        public string? Name { get; set; }
        public string? outputType { get; set; }
        public string? Filepath { get; set; }
        //public string? ExtInfo { get; set; }
        public float StartFrame { get; set; }
        public int FrameRange { get; set; }
        public int FrameStep { get; set; }
        public bool GPU { get; set; }
        public bool OW { get; set; }
        public bool vid { get; set; }
        public string? RenderApp { get; set; }
        public int Priority { get; set; }
        //public int Samples { get; set; }
        public int split { get; set; }
        //public int MaxRenderTime { get; set; }
        public int QueueIndex { get; set; }

    }
}
