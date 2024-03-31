using Newtonsoft.Json;

namespace FlagShip_Manager.Objects
{
    public class Path_Settings
    {
        //Stores the three paths flagship needs to function. 
        //Setup on first launch. 

        public string TempOutputPath { get; set; } = "";
        public string TempProjectPath { get; set; } = "";
        public string CtlFolder { get; set; } = "";

        public Path_Settings(string _tmpOutputPath = "", string _tempProjectPath = "", string _ctlFolder = "")
        {
            //Builds Path_Settings object.

            TempOutputPath = _tmpOutputPath;
            TempProjectPath = _tempProjectPath;
            CtlFolder = _ctlFolder;
        }
        public void Save(string _filePath = "")
        {
            //Saves Path_Setting Object into users Documents folder for later use. 
            //Stores in plain text.

            if (_filePath == "") _filePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_Settings.txt";
            try
            {
                string output = JsonConvert.SerializeObject(this);
                File.WriteAllText(_filePath, output);

            }
            catch (Exception EX)
            {
                Console.WriteLine("Failed to save Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                Console.WriteLine(EX.Message);
            }
        }
        public bool Load(string _filePath = "")
        {
            //Loads existing Path_Settings file.

            if (_filePath == "") _filePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\documents\\FlagShip_Settings.txt";
            if (!File.Exists(_filePath)) return false;
            Path_Settings Input = new Path_Settings();
            string tempString = File.ReadAllText(_filePath);
            if (File.Exists(_filePath))
            {
                try
                {
                    Input = JsonConvert.DeserializeObject<Path_Settings>(tempString);
                }

                catch (Exception EX)
                {

                    Console.WriteLine("Failed to read Database. Possibly not able to access file.\nERROR MESSAGE:\n");
                    Console.WriteLine(EX.Message);

                }
                if (Input.CheckSettings())
                {
                    TempOutputPath = Input.TempOutputPath;
                    TempProjectPath = Input.TempProjectPath;
                    CtlFolder = Input.CtlFolder;
                    return true;
                }
            }
            return false;

        }
        public bool CheckSettings()
        {
            //Checks if Path_Settings object is valid.

            if (TempOutputPath == "") return false;
            if (TempProjectPath == "") return false;
            if (CtlFolder == "") return false;
            else return true;

        }
    }
}
