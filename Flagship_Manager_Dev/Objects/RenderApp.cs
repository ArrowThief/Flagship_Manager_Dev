namespace FlagShip_Manager.Objects
{
    public class RenderApp
    {
        //Stores render app info for each of the three render apps
        //TODO: Merge this into Job and jobGUI or set it with JS and HTML.

        public string ImagePath { get; set; } = "";
        public string AppName { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool Default { get; set; } = false;

        public void EnableDisable()
        {
            Enabled = !Enabled;
            GetAppIcon();
        }
        public void GetAppIcon()
        {
            if (Enabled)
            {
                switch (AppName)
                {
                    case "ae":
                        ImagePath = "Images/App/ae.png";
                        return;
                    case "blender":
                        ImagePath = "Images/App/blender.png";
                        return;
                    case "fusion":
                        ImagePath = "Images/App/fusion.png";
                        return;
                }
            }
            else
            {
                switch (AppName)
                {
                    case "ae":
                        ImagePath = "Images/App/ae_bw.png";
                        return;
                    case "blender":
                        ImagePath = "Images/App/blender_bw.png";
                        return;
                    case "fusion":
                        ImagePath = "Images/App/fusion_bw.png";
                        return;
                }
            }
        }
    }
}
