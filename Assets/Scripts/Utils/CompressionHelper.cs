using System.IO;
using System.IO.Compression;
using System.Text;

public static class CompressionHelper
{
    public static byte[] CompressString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return memoryStream.ToArray();
        }
    }

    public static string DecompressBytes(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        {
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                using (var streamReader = new StreamReader(gzipStream, Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
