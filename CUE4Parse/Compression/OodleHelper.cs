using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
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
    public const string OODLE_NAME_OLD = "oo2core_9_win64.dll";
    public const string OODLE_NAME_CURRENT = "oodle-data-shared.dll";
    public const string OODLE_NAME_LINUX = "liboodle-data-shared.so";

    private const string RELEASE_URL = "https://github.com/WorkingRobot/OodleUE/releases/download/2026-01-25-1223";
    private const string WINDOWS_ZIP = "clang-cl-x64-release.zip";
    private const string LINUX_ZIP = "gcc-x64-release.zip";

    public static string OodleFileName => OperatingSystem.IsLinux() ? OODLE_NAME_LINUX : OODLE_NAME_CURRENT;
    public static Oodle? Instance { get; private set; }

    public static void Initialize(string? path = null) =>
        InitializeAsync(path).GetAwaiter().GetResult();

    public static async Task InitializeAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        if (Instance is not null) return;

        if (path is null && CUE4ParseNatives.IsFeatureAvailable("Oodle\0"u8))
        {
            Initialize(new Oodle(NativeLibrary.Load(CUE4ParseNatives.LibraryName)));
            return;
        }

        var oodlePath = path;
        if (await DownloadOodleDllAsync(ref oodlePath, cancellationToken).ConfigureAwait(false) &&
            !string.IsNullOrWhiteSpace(oodlePath))
        {
            Initialize(new Oodle(oodlePath));
            return;
        }

        Log.Warning("Oodle decompression failed: unable to download oodle dll");
    }

    public static void Initialize(Oodle instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static bool DownloadOodleDll() =>
        DownloadOodleDllAsync().GetAwaiter().GetResult();

    public static bool DownloadOodleDll(ref string? path) =>
        DownloadOodleDllAsync(ref path).GetAwaiter().GetResult();

    public static void Decompress(
        byte[] compressed,   int compressedOffset,   int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize,
        FArchive? reader = null)
    {
        var instance = Instance;
        if (instance is null)
        {
            ThrowDecompressionException(reader, "Oodle decompression failed: not initialized");
        }

        var decodedSize = instance.Decompress(compressed.AsSpan(compressedOffset, compressedSize),
            uncompressed.AsSpan(uncompressedOffset, uncompressedSize));

        if (decodedSize <= 0)
        {
            ThrowDecompressionException(reader, $"Oodle decompression failed with result {decodedSize}");
        }

        if (decodedSize < uncompressedSize)
        {
            // Not sure whether this should be an exception or not
            Log.Warning("Oodle decompression just decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
        }
    }

    public static Task<bool> DownloadOodleDllAsync(CancellationToken cancellationToken = default)
    {
        string? path = null;
        return DownloadOodleDllAsync(ref path, cancellationToken);
    }

    public static Task<bool> DownloadOodleDllAsync(ref string? path, CancellationToken cancellationToken = default)
    {
        path = ResolvePath(path);
        return File.Exists(path)
            ? Task.FromResult(true)
            : DownloadOodleDllFromOodleUEAsync(HttpUtils.DownloadClient, path, cancellationToken);
    }

    public static async Task<bool> DownloadOodleDllFromOodleUEAsync(HttpClient client, string path, CancellationToken cancellationToken = default)
    {
        var (url, entryName) = OperatingSystem.IsLinux()
            ? ($"{RELEASE_URL}/{LINUX_ZIP}", $"lib/{OODLE_NAME_LINUX}")
            : ($"{RELEASE_URL}/{WINDOWS_ZIP}", $"bin/{OODLE_NAME_CURRENT}");

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var zip = new ZipArchive(responseStream, ZipArchiveMode.Read);
            var entry = zip.GetEntry(entryName);
            ArgumentNullException.ThrowIfNull(entry, "oodle entry in zip not found");
            await using var entryStream = entry.Open();
            await using var fs = File.Create(path);
            await entryStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);

            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                           UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                           UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Uncaught exception while downloading oodle dll from OodleUE");
        }

        return false;
    }

    private static string ResolvePath(string? path)
    {
        if (!OperatingSystem.IsLinux() && File.Exists(OODLE_NAME_OLD))
        {
            return OODLE_NAME_OLD;
        }

        return string.IsNullOrWhiteSpace(path) ? OodleFileName : path;
    }

    [DoesNotReturn]
    private static void ThrowDecompressionException(FArchive? reader, string message)
    {
        if (reader is not null) throw new OodleException(reader, message);
        throw new OodleException(message);
    }
}
