using System.IO;
using System.IO.Compression;
using System.Text;

namespace sfintegration.infrastructure.Helper
{
    public static class CompressionHelper
    {
        public static MemoryStream CompressToStream(byte[] bytes)
        {
            var ms = new MemoryStream();

            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            ms.Position = 0;
            return ms;
        }

        public static MemoryStream DecompressToStream(MemoryStream source)
        {
            source.Position = 0;
            using (var decompressed = new GZipStream(source, CompressionMode.Decompress))
            {
                var outStream = new MemoryStream();

                decompressed.CopyTo(outStream);
                return outStream;
            }
        }

        public static void CompressToFile(string source, string fileDestination)
        {
            var byteSource = Encoding.ASCII.GetBytes(source);
            var compressed = Compress(byteSource);

            File.WriteAllBytes(fileDestination, compressed);
        }

        public static byte[] CompressToBytes(string fileSource)
        {
            var byteSource = Encoding.ASCII.GetBytes(fileSource);

            return Compress(byteSource);
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                return memory.ToArray();
            }
        }
    }
}
