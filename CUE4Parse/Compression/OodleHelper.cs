using System;
using System.IO;
using System.IO.Compression;
using System.Net;
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
        path ??= OODLE_DLL_NAME;

        using var client = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All
        });
        client.Timeout = TimeSpan.FromSeconds(20);

        try
        {
            using var indexResponse = await client.GetAsync(WARFRAME_INDEX_URL).ConfigureAwait(false);
            indexResponse.EnsureSuccessStatusCode();
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

            {
                await using var dllFs = File.Create(path);
                await dllStream.CopyToAsync(dllFs).ConfigureAwait(false);
            }

            Log.Information($"Successfully downloaded oodle dll at \"{path}\"");
            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Uncaught exception while downloading oodle dll");
        }

        Log.Information("Downloading oodle from alternative source");

        var altResult = await DownloadOodleDllFromOodleUEAsync(client, path).ConfigureAwait(false);
        return altResult;
    }

    public static async Task<bool> DownloadOodleDllFromOodleUEAsync(HttpClient client, string path)
    {
        const string url = "https://github.com/WorkingRobot/OodleUE/releases/download/2024-11-01-726/msvc.zip"; // 2.9.13
        const string entryName = "bin/Release/oodle-data-shared.dll";

        try
        {
            using var response = await client.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var zip = new ZipArchive(responseStream, ZipArchiveMode.Read);
            var entry = zip.GetEntry(entryName);
            ArgumentNullException.ThrowIfNull(entry, "oodle entry in zip not found");
            await using var entryStream = entry.Open();
            await using var fs = File.Create(path);
            await entryStream.CopyToAsync(fs).ConfigureAwait(false);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Uncaught exception while downloading oodle dll from OodleUE");
        }
        return false;
    }
}
