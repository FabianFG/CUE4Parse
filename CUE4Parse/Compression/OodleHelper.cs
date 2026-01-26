using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
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
    public const string OODLE_DLL_NAME_OLD = "oo2core_9_win64.dll";
    public const string OODLE_DLL_NAME = "oodle-data-shared.dll";

    public static Oodle? Instance { get; private set; }

    public static void Initialize(string? path = null)
    {
        if (Instance is not null) return;

        if (path is null && CUE4ParseNatives.IsFeatureAvailable("Oodle\0"u8))
        {
            Instance = new Oodle(NativeLibrary.Load(CUE4ParseNatives.LibraryName));
        }
        else
        {
            path ??= OODLE_DLL_NAME_OLD;
            if (DownloadOodleDll(path))
            {
                Instance = new Oodle(path);
            }
            else
            {
                Log.Warning("Oodle decompression failed: unable to download oodle dll");
            }
        }
    }

    public static void Initialize(Oodle instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static bool DownloadOodleDll(string? path = null)
    {
        path ??= OODLE_DLL_NAME_OLD;
        return File.Exists(path) || DownloadOodleDllAsync(path).GetAwaiter().GetResult();
    }

    public static void Decompress(
        byte[] compressed,   int compressedOffset,   int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize,
        FArchive? reader = null)
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
        path ??= OODLE_DLL_NAME_OLD;

        using var client = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All
        });
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(
            nameof(CUE4Parse),
            typeof(OodleHelper).Assembly.GetName().Version?.ToString() ?? "1.0.0"));
        client.Timeout = TimeSpan.FromSeconds(30);

        return await DownloadOodleDllFromOodleUEAsync(client, path).ConfigureAwait(false);
    }

    public static async Task<bool> DownloadOodleDllFromOodleUEAsync(HttpClient client, string path)
    {
        const string url = "https://github.com/WorkingRobot/OodleUE/releases/download/2026-01-21-1216/clang-cl-release.zip"; // 2.9.15
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
