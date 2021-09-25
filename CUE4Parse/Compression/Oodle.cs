using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

using Serilog;

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CUE4Parse.Compression
{
    public class OodleException : ParserException
    {
        public OodleException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
        public OodleException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
    }

    public static class Oodle
    {
        public unsafe delegate long OodleDecompress(byte* bufferPtr, long bufferSize, byte* outputPtr, long outputSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int threadModule);

        private const string WARFRAME_CDN_HOST = "https://origin.warframe.com";
        private const string WARFRAME_INDEX_PATH = "/origin/E926E926/index.txt.lzma";
        private static string WARFRAME_INDEX_URL => WARFRAME_CDN_HOST + WARFRAME_INDEX_PATH;
        public const string OODLE_DLL_NAME = "oo2core_9_win64.dll";

        public static OodleDecompress DecompressFunc;

        static unsafe Oodle()
        {
            DecompressFunc = OodleLZ_Decompress;
        }

        public static bool LoadOodleDll(string? path = null)
        {
            if (File.Exists(OODLE_DLL_NAME)) return true;
            return DownloadOodleDll(path).GetAwaiter().GetResult();
        }

        public static unsafe void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
                                             byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
        {
            if (DecompressFunc == OodleLZ_Decompress)
                LoadOodleDll();

            long decodedSize;

            fixed (byte* compressedPtr = compressed, uncompressedPtr = uncompressed)
            {
                decodedSize = DecompressFunc(compressedPtr + compressedOffset, compressedSize,
                                             uncompressedPtr + uncompressedOffset, uncompressedSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
            }

            if (decodedSize <= 0)
            {
                if (reader != null) throw new OodleException(reader, $"Oodle decompression failed with result {decodedSize}");
                throw new OodleException($"Oodle decompression failed with result {decodedSize}");
            }

            if (decodedSize < uncompressedSize)
            {
                // Not sure whether this should be an exception or not
                Log.Warning("Oodle decompression just decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
            }
        }

        [DllImport(OODLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern long OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] output, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int threadModule);
        [DllImport(OODLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe long OodleLZ_Decompress(byte* buffer, long bufferSize, byte* output, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int threadModule);

        public static async Task<bool> DownloadOodleDll(string? path)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            try
            {
                using var indexResponse = await client.GetAsync(WARFRAME_INDEX_URL).ConfigureAwait(false);
                await using var indexLzmaStream = await indexResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var indexStream = new MemoryStream();

                Lzma.Decompress(indexLzmaStream, indexStream);
                indexStream.Position = 0;

                string? dllUrl = null;
                using var indexReader = new StreamReader(indexStream);
                while (!indexReader.EndOfStream)
                {
                    var line = await indexReader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.Contains(OODLE_DLL_NAME))
                    {
                        dllUrl = WARFRAME_CDN_HOST + line[..line.IndexOf(',')];
                        break;
                    }
                }

                if (dllUrl == null)
                {
                    Log.Warning("Warframe index did not contain oodle dll");
                    return false;
                }

                using var dllResponse = await client.GetAsync(dllUrl).ConfigureAwait(false);
                await using var dllLzmaStream = await dllResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using var dllStream = new MemoryStream();

                Lzma.Decompress(dllLzmaStream, dllStream);
                dllStream.Position = 0;
                var dllPath = path ?? OODLE_DLL_NAME;
                var dllFs = File.Create(dllPath);
                await dllStream.CopyToAsync(dllFs).ConfigureAwait(false);
                await dllFs.DisposeAsync().ConfigureAwait(false);
                Log.Information($"Successfully downloaded oodle dll at \"{dllPath}\"");
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Uncaught exception while downloading oodle dll");
            }
            return false;
        }
    }
}