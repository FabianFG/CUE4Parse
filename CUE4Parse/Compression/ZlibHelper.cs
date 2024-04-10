using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

using Serilog;

using ZlibngDotNet;

namespace CUE4Parse.Compression;

public class ZlibException : ParserException
{
    public ZlibException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    public ZlibException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
}

public static class ZlibHelper
{
    public const string DOWNLOAD_URL = "https://github.com/NotOfficer/Zlib-ng.NET/releases/download/1.0.0/zlib-ng2.dll";
    public const string DLL_NAME = "zlib-ng2.dll";

    public static Zlibng? Instance { get; private set; }

    public static void Initialize(string path)
    {
        Instance?.Dispose();
        Instance = new Zlibng(path);
    }

    public static void Initialize(Zlibng instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static bool DownloadDll(string? path = null, string? url = null)
    {
        if (File.Exists(path ?? DLL_NAME)) return true;
        return DownloadDllAsync(path, url).GetAwaiter().GetResult();
    }

    public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
    {
        if (Instance is null)
        {
            const string message = "Zlib decompression failed: not initialized";
            if (reader is not null) throw new ZlibException(reader, message);
            throw new ZlibException(message);
        }

        var result = Instance.Uncompress(uncompressed.AsSpan(uncompressedOffset, uncompressedSize),
            compressed.AsSpan(compressedOffset, compressedSize), out int decodedSize);

        if (result != ZlibngCompressionResult.Ok)
        {
            var message = $"Zlib decompression failed with result {result}";
            if (reader is not null) throw new ZlibException(reader, message);
            throw new ZlibException(message);
        }

        if (decodedSize < uncompressedSize)
        {
            // Not sure whether this should be an exception or not
            Log.Warning("Oodle decompression just decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
        }
    }

    public static async Task<bool> DownloadDllAsync(string? path, string? url = null)
    {
        using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false, UseCookies = false });
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            var dllPath = path ?? DLL_NAME;
            {
                using var dllResponse = await client.GetAsync(url ?? DOWNLOAD_URL).ConfigureAwait(false);
                await using var dllFs = File.Create(dllPath);
                await dllResponse.Content.CopyToAsync(dllFs).ConfigureAwait(false);
            }
            Log.Information($"Successfully downloaded zlib-ng dll at \"{dllPath}\"");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Uncaught exception while downloading zlib-ng dll");
        }
        return false;
    }
}
