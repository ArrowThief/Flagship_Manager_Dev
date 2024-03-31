namespace FlagShip_Manager.Objects
{
    public class importRender
    {
        //Object matching output from After effects and Blender
        //TODO: Merge this into Job such that it doesn't need to exits as a seprate class.

        public string? Project { get; set; }
        public string? WorkingProject { get; set; }
        public string? Name { get; set; }
        public string? outputType { get; set; }
        public string? Filepath { get; set; }
        public float StartFrame { get; set; }
        public int FrameRange { get; set; }
        public int FrameStep { get; set; }
        public bool GPU { get; set; }
        public bool OW { get; set; }
        public bool vid { get; set; }
        public string? RenderApp { get; set; }
        public int Priority { get; set; }
        public int split { get; set; }
        public int QueueIndex { get; set; }

    }
}
