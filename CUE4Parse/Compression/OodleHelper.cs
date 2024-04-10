using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

using OodleDotNet;

using Serilog;

namespace CUE4Parse.Compression;

public class OodleException : ParserException
{
    public OodleException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    public OodleException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
}

public static class OodleHelper
{
    private const string WARFRAME_CONTENT_HOST = "https://content.warframe.com";
    private const string WARFRAME_ORIGIN_HOST = "https://origin.warframe.com";
    private const string WARFRAME_INDEX_PATH = "/origin/50F7040A/index.txt.lzma";
    private const string WARFRAME_INDEX_URL = WARFRAME_ORIGIN_HOST + WARFRAME_INDEX_PATH;
    public const string OODLE_DLL_NAME = "oo2core_9_win64.dll";

    public static Oodle? Instance { get; private set; }

    public static void Initialize(string path)
    {
        Instance?.Dispose();
        Instance = new Oodle(path);
    }

    public static void Initialize(Oodle instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static bool DownloadOodleDll(string? path = null)
    {
        if (File.Exists(path ?? OODLE_DLL_NAME)) return true;
        return DownloadOodleDllAsync(path).GetAwaiter().GetResult();
    }

    public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
    {
        if (Instance is null)
        {
            const string message = "Oodle decompression failed: not initialized";
            if (reader is not null) throw new OodleException(reader, message);
            throw new OodleException(message);
        }

        var decodedSize = Instance.Decompress(compressed.AsSpan(compressedOffset, compressedSize),
            uncompressed.AsSpan(uncompressedOffset, uncompressedSize));

        if (decodedSize <= 0)
        {
            var message = $"Oodle decompression failed with result {decodedSize}";
            if (reader is not null) throw new OodleException(reader, message);
            throw new OodleException(message);
        }

        if (decodedSize < uncompressedSize)
        {
            // Not sure whether this should be an exception or not
            Log.Warning("Oodle decompression just decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
        }
    }

    public static async Task<bool> DownloadOodleDllAsync(string? path)
    {
        using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false, UseCookies = false });
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            using var indexResponse = await client.GetAsync(WARFRAME_INDEX_URL).ConfigureAwait(false);
            await using var indexLzmaStream = await indexResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var indexStream = new MemoryStream();

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
                    dllUrl = WARFRAME_CONTENT_HOST + line[..line.IndexOf(',')];
                    break;
                }
            }

            if (dllUrl == null)
            {
                Log.Warning("Warframe index did not contain oodle dll");
                return false;
            }

            using var dllResponse = await client.GetAsync(dllUrl).ConfigureAwait(false);
            var dllStream = new MemoryStream();
            await using var dllLzmaStream = await dllResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            Lzma.Decompress(dllLzmaStream, dllStream);
            dllStream.Position = 0;

            var dllPath = path ?? OODLE_DLL_NAME;
            {
                await using var dllFs = File.Create(dllPath);
                await dllStream.CopyToAsync(dllFs).ConfigureAwait(false);
            }
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
