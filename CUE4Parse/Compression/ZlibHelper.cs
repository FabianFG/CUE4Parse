using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

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
    public const string DOWNLOAD_URL = "https://github.com/NotOfficer/Zlib-ng.NET/releases/download/1.0.0/zlib-ng2.dll.gz";
    public const string DOWNLOAD_URL_LINUX = "https://github.com/NotOfficer/Zlib-ng.NET/releases/download/1.0.0/libz-ng.so.gz";
    public const string DLL_NAME = "zlib-ng2.dll";
    public const string DLL_NAME_LINUX = "libz-ng.so";

    public static Zlibng? Instance { get; private set; }
    public static string DllName => OperatingSystem.IsLinux() ? DLL_NAME_LINUX : DLL_NAME;

    public static void Initialize(string? path = null)
        => InitializeAsync(path).GetAwaiter().GetResult();

    public static async Task InitializeAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        if (Instance is not null) return;

        var dllPath = string.IsNullOrWhiteSpace(path) ? DllName : path;
        if (!await DownloadDllAsync(dllPath, null, cancellationToken).ConfigureAwait(false))
        {
            Log.Warning("Zlib decompression failed: unable to download zlib-ng dll");
            return;
        }

        Initialize(new Zlibng(dllPath));
    }

    public static void Initialize(Zlibng instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static bool DownloadDll(string? path = null, string? url = null)
        => DownloadDllAsync(path, url).GetAwaiter().GetResult();

    public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
    {
        var instance = Instance;
        if (instance is null)
        {
            ThrowDecompressionException(reader, "Zlib decompression failed: not initialized");
        }

        var result = instance.Uncompress(uncompressed.AsSpan(uncompressedOffset, uncompressedSize),
            compressed.AsSpan(compressedOffset, compressedSize), out int decodedSize);

        if (result != ZlibngCompressionResult.Ok)
        {
            ThrowDecompressionException(reader, $"Zlib decompression failed with result {result}");
        }

        if (decodedSize < uncompressedSize)
        {
            // Not sure whether this should be an exception or not
            Log.Warning("Zlib decompression only decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
        }
    }

    public static Task<bool> DownloadDllAsync(string? path, string? url = null)
        => DownloadDllAsync(path, url, CancellationToken.None);

    public static async Task<bool> DownloadDllAsync(string? path, string? url, CancellationToken cancellationToken)
    {
        var dllPath = string.IsNullOrWhiteSpace(path) ? DllName : path;
        if (File.Exists(dllPath)) return true;

        var resolvedUrl = ResolveDownloadUrl(url);
        try
        {
            using var dllResponse = await HttpUtils.DownloadClient
                .GetAsync(resolvedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            dllResponse.EnsureSuccessStatusCode();

            await using var dllFs = File.Create(dllPath);
            if (resolvedUrl.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                await using var contentStream = await dllResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var gzipStream = new GZipStream(contentStream, CompressionMode.Decompress, leaveOpen: false);
                await gzipStream.CopyToAsync(dllFs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await using var contentStream = await dllResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await contentStream.CopyToAsync(dllFs, cancellationToken).ConfigureAwait(false);
            }

            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(dllPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                              UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                              UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            Log.Information("Successfully downloaded Zlib-ng dll at {0}", dllPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Uncaught exception while downloading Zlib-ng dll");
        }
        return false;
    }

    private static string ResolveDownloadUrl(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url)) return url;
        return OperatingSystem.IsLinux() ? DOWNLOAD_URL_LINUX : DOWNLOAD_URL;
    }

    [DoesNotReturn]
    private static void ThrowDecompressionException(FArchive? reader, string message)
    {
        if (reader is not null) throw new ZlibException(reader, message);
        throw new ZlibException(message);
    }
}
