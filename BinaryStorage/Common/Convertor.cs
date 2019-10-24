using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BinStorage.Common
{
    public class Convertor
    {

        #region Methods

        public async Task<byte[]> GetMd5HashAsync(Stream stream)
        {
            using (MD5 md5 = MD5.Create())
            {
                return await Task.Run(() => md5.ComputeHash(stream));
            }
        }

        public async Task<byte[]> ConvertStreamToByteArrayAsync(Stream inputStream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                inputStream.Position = 0;
                await inputStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public Stream ConvertByteArrayToStream(byte[] byteArray)
        {
            return new MemoryStream(byteArray);
        }

        public async Task<byte[]> CompressAsync(Stream inputStream)
        {
            using (var compressedStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Compress, true))
                {
                    inputStream.Position = 0;
                    await inputStream.CopyToAsync(deflateStream);
                }
                return compressedStream.ToArray();
            }
        }

        public async Task<Stream> DecompressAsync(byte[] data)
        {
            var outputStream = new MemoryStream();
            using (var compressedStream = new MemoryStream(data))
            {
                using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                {
                    await deflateStream.CopyToAsync(outputStream);
                }
            }
            outputStream.Position = 0;
            return outputStream;
        }

        public byte[] ConvertIntToByteArray(int number)
        {
            return BitConverter.GetBytes(number);
        }

        public int ConvertByteArrayToInt(byte[] byteArray)
        {
            return BitConverter.ToInt32(byteArray, 0);
        }

        #endregion /Methods

    }
}
