using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Runtime.InteropServices;

using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

using OodleDotNet;


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

    private const string RELEASE_URL = "https://github.com/WorkingRobot/OodleUE/releases/download/2026-06-04-1357"; // 2.9.16
    private const string WINDOWS_ZIP = "clang-cl-x64-release.zip";
    private const string LINUX_ZIP = "gcc-x64-release.zip";

    /// <summary>
    /// First byte of a compressed block header, shared by every Oodle codec.
    /// </summary>
    private const byte BLOCK_HEADER_MARKER = 0x8C;

    /// <summary>
    /// Sub-code of a plain Leviathan block, what UE's Oodle wrapper writes by default.
    /// </summary>
    private const byte BLOCK_HEADER_LEVIATHAN = 0x0C;

    /// <summary>
    /// Leviathan sub-code written by some UE 5.5 builds. The payload is plain Leviathan but Oodle
    /// versions predating the one bundled with the game reject the unknown sub-code.
    /// </summary>
    private const byte BLOCK_HEADER_LEVIATHAN_ALT = 0x14;

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
        Compression.UseNativeOodle(instance);
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

        var compressedSpan = compressed.AsSpan(compressedOffset, compressedSize);
        var uncompressedSpan = uncompressed.AsSpan(uncompressedOffset, uncompressedSize);

        var decodedSize = instance.Decompress(compressedSpan, uncompressedSpan);

        if (decodedSize <= 0 && TryPatchNonStandardBlockHeader(compressedSpan) is { } patched)
        {
            try
            {
                decodedSize = instance.Decompress(patched.AsSpan(0, compressedSize), uncompressedSpan);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(patched);
            }
        }

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

    /// <summary>
    /// Whether <paramref name="compressed"/> starts with a block header using the alternate
    /// Leviathan sub-code, which older Oodle versions refuse to decode.
    /// </summary>
    public static bool IsNonStandardBlockHeader(ReadOnlySpan<byte> compressed) =>
        compressed.Length > 1 && compressed[0] == BLOCK_HEADER_MARKER && compressed[1] == BLOCK_HEADER_LEVIATHAN_ALT;

    /// <summary>
    /// Rents a copy of <paramref name="compressed"/> whose block header carries the standard
    /// Leviathan sub-code, or <c>null</c> when the header is already standard. Only the first
    /// <paramref name="compressed"/>.Length bytes are meaningful and the caller is responsible for
    /// returning the buffer to <see cref="ArrayPool{T}.Shared"/>.
    /// </summary>
    public static byte[]? TryPatchNonStandardBlockHeader(ReadOnlySpan<byte> compressed)
    {
        if (!IsNonStandardBlockHeader(compressed)) return null;

        var patched = ArrayPool<byte>.Shared.Rent(compressed.Length);
        compressed.CopyTo(patched);
        patched[1] = BLOCK_HEADER_LEVIATHAN;
        return patched;
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
        (string? url, string? entryName) = OperatingSystem.IsLinux()
            ? ($"{RELEASE_URL}/{LINUX_ZIP}", $"lib/{OODLE_NAME_LINUX}")
            : ($"{RELEASE_URL}/{WINDOWS_ZIP}", $"bin/{OODLE_NAME_CURRENT}");

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var zip = await ZipArchive.CreateAsync(responseStream, ZipArchiveMode.Read, true, null, cancellationToken).ConfigureAwait(false);
            var entry = zip.GetEntry(entryName);
            ArgumentNullException.ThrowIfNull(entry, "oodle entry in zip not found");
            await using var entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
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
        return Path.GetFullPath(!string.IsNullOrWhiteSpace(path)
            ? path
            : !OperatingSystem.IsLinux() && File.Exists(OODLE_NAME_OLD) ? OODLE_NAME_OLD : OodleFileName);
    }

    [DoesNotReturn]
    private static void ThrowDecompressionException(FArchive? reader, string message)
    {
        if (reader is not null) throw new OodleException(reader, message);
        throw new OodleException(message);
    }
}
