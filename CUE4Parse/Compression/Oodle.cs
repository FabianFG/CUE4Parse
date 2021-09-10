using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.Compression
{
    public class OodleException : ParserException
    {
        public OodleException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
        
        public OodleException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
    }
    public static class Oodle
    {
        private const string WARFRAME_CDN_HOST = "https://origin.warframe.com";
        private const string WARFRAME_INDEX_PATH = "/origin/E926E926/index.txt.lzma";
        private const string WARFRAME_INDEX_URL = WARFRAME_CDN_HOST + WARFRAME_INDEX_PATH;
        public const string OODLE_DLL_NAME = "oo2core_9_win64.dll";
        
        public static bool LoadOodleDll()
        {
            if (File.Exists(OODLE_DLL_NAME))
            {
                return true;
            }
            return DownloadOodleDll().Result;
        }
        
        public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
            byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
        {
            LoadOodleDll();
            unsafe
            {
                fixed (byte* compressedPtr = compressed, uncompressedPtr = uncompressed)
                {
                    var decodedSize = OodleLZ_Decompress(compressedPtr + compressedOffset, compressedSize,
                        uncompressedPtr + uncompressedOffset, uncompressedSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    
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
            }
        }
        
        [DllImport(OODLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern long OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] result, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int ThreadModule);
        [DllImport(OODLE_DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe long OodleLZ_Decompress(byte* buffer, long bufferSize, byte* result, long outputBufferSize, int a, int b, int c, long d, long e, long f, long g, long h, long i, int ThreadModule);

        private static async Task<bool> DownloadOodleDll()
        {
            using var client = new HttpClient {Timeout = TimeSpan.FromSeconds(2)};
            using HttpRequestMessage request = new (HttpMethod.Get, new Uri(WARFRAME_INDEX_URL));
            try
            {
                using var httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var lzma = await httpResponseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var input = new MemoryStream(lzma);
                var output = new MemoryStream();
                
                Lzma.Decompress(input, output);
                output.Position = 0;
                
                string? line, dllUrl = null;
                using var reader = new StreamReader(output);
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.Contains(OODLE_DLL_NAME))
                    {
                        dllUrl = WARFRAME_CDN_HOST + line[..line.IndexOf(',')];
                        break;
                    }
                }
                
                if (dllUrl == null)
                {
                    Log.Warning("Warframe index did not contain oodle dll");
                    return default;
                }

                using var dllRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(dllUrl));
                using var dllResponse = await client.SendAsync(dllRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var dllLzma = await dllResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                input = new MemoryStream(dllLzma);
                output = new MemoryStream();
                
                Lzma.Decompress(input, output);
                output.Position = 0;
                
                await File.WriteAllBytesAsync(OODLE_DLL_NAME, output.ToArray());
                Log.Information($"Successfully downloaded oodle dll at \"{OODLE_DLL_NAME}\"");
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Uncaught exception while downloading oodle dll");
            }
            return default;
        }
    }
}