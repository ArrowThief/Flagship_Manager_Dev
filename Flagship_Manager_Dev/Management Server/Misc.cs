using System.IO.Compression;

namespace FlagShip_Manager.Management_Server
{
    public class Misc
    {
        //A miscellaneous class for methods that don't fit with any single classes. 

        internal static byte[] CompressArray(byte[] _unCompressedArray)
        {
            //returns a Compressesed byte[].

            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(_unCompressedArray, 0, _unCompressedArray.Length);
            }
            return output.ToArray();
        }
        internal static byte[] DeCompressArray(byte[] _CompressedArray)
        {

            //returns a Decompressesed byte[].

            MemoryStream input = new MemoryStream(_CompressedArray);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}

