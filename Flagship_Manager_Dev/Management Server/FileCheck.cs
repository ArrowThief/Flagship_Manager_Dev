using FFMpegCore;

namespace FlagShip_Manager.Management_Server
{
    public class FileCheck
    {
        private static readonly int[] size1080 = { 16208, 4173, 5139, 563, 1578, 4941, 5776, 8100, 8118 };
        private static readonly int[] size4k = { 64808, 10607, 17031, 1755, 6314, 16633, 19330, 32400, 32417 };
        private static readonly string[] FileType = { "dpx", "exr", "iff", "jpg", "png", "psd", "sgi", "tga", "tif" };


        public static bool CheckFileSize(string _path, bool large = false)
        {
            foreach (var file in Directory.GetFiles(_path))
            {
                FileInfo info = new FileInfo(file);
                var KB = info.Length / 1024;
                var type = Path.GetExtension(file).Substring(1);
                int typeIndex = Array.IndexOf(FileType, type);
                int ExpectedFileSize;
                if (large)
                {
                    ExpectedFileSize = size4k[typeIndex];
                }
                else
                {
                    ExpectedFileSize = size1080[typeIndex];
                }


                if (KB >= ExpectedFileSize)
                {
                    Console.WriteLine($"Good! FileSize: {KB} Expected: {ExpectedFileSize}");
                    //return true;
                }
                else
                {
                    Console.WriteLine($"Bad! FileSize: {KB} Expected: {ExpectedFileSize}");
                    //return false;
                }
            }
            return false;

        }
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }
        public static bool CheckFileReadability(string FilePath)
        {
            try
            {
                FFProbe.Analyse(FilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }

}
