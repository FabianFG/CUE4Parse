using System.Runtime.InteropServices;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.Compression;

public static class OodleTextureHelper
{
    public const string OODLE_TEXRT_NAME = "oo2texrt_win64_2.9.16.dll";

    private const string TEXRT_URL = "https://github.com/WorkingRobot/OodleUE/raw/refs/heads/main/Engine/Source/Developer/TextureFormatOodle/Sdks/2.9.16/redist/Win64/oo2texrt_win64_2.9.16.dll";

    private static nint _handle;
    private static OodleTexRtBc7PrepDecode? _bc7PrepDecode;
    private static OodleTexRtBc7PrepReadHeader? _bc7PrepReadHeader;
    private static OodleTexRtBc7PrepMinDecodeScratchSize? _bc7PrepMinDecodeScratchSize;

    private struct OodleTexRtBc7PrepHeader
    {
        public uint Version;
        public uint Flags;
        public unsafe fixed uint ModeCounts[10];
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int OodleTexRtBc7PrepReadHeader(
        OodleTexRtBc7PrepHeader* header,
        nint* outNumBlocks,
        nint* outPayloadSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint OodleTexRtBc7PrepMinDecodeScratchSize(nint numBlocks);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int OodleTexRtBc7PrepDecode(
        byte* outputBuf,
        long outputBufSize,
        byte* bc7prepData,
        long bc7prepDataSize,
        OodleTexRtBc7PrepHeader* header,
        int flags,
        byte* scratchBuf,
        long scratchSize);

    public static void Initialize(string? path = null) =>
        InitializeAsync(path).GetAwaiter().GetResult();

    public static async Task InitializeAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        if (_bc7PrepDecode is not null) return;

        var dllPath = path;
        if (await DownloadOodleTextureDllAsync(ref dllPath, cancellationToken).ConfigureAwait(false) &&
            !string.IsNullOrWhiteSpace(dllPath))
        {
            InitializeFromPath(dllPath);
            return;
        }

        Log.Warning("Oodle Texture Runtime initialization failed: unable to load {Name}", OODLE_TEXRT_NAME);
    }

    public static unsafe bool TryDecodeBc7Prep(byte[] source, byte[] destination, int version, uint flags, int[] modeCounts)
    {
        if (modeCounts.Length < 10) return false;
        if (_bc7PrepDecode is null) Initialize();
        if (_bc7PrepDecode is null || _bc7PrepReadHeader is null || _bc7PrepMinDecodeScratchSize is null) return false;

        fixed (byte* src = source)
        fixed (byte* dst = destination)
        {
            var header = new OodleTexRtBc7PrepHeader
            {
                Version = (uint) version,
                Flags = flags
            };

            for (var i = 0; i < 10; i++)
                header.ModeCounts[i] = (uint) modeCounts[i];

            nint numBlocks = 0;
            nint payloadSize = 0;
            var headerResult = _bc7PrepReadHeader(&header, &numBlocks, &payloadSize);
            if (headerResult != 0 || numBlocks <= 0 || payloadSize <= 0 || payloadSize > source.Length)
                return false;

            var scratchSize = _bc7PrepMinDecodeScratchSize(numBlocks);
            if (scratchSize <= 0) return false;

            var scratch = new byte[(int) scratchSize];
            fixed (byte* scratchPtr = scratch)
            {
                var decoded = _bc7PrepDecode(dst, destination.Length, src, source.Length, &header, 0, scratchPtr, scratch.Length);
                return decoded > 0;
            }
        }
    }

    public static Task<bool> DownloadOodleTextureDllAsync(CancellationToken cancellationToken = default)
    {
        string? path = null;
        return DownloadOodleTextureDllAsync(ref path, cancellationToken);
    }

    public static Task<bool> DownloadOodleTextureDllAsync(ref string? path, CancellationToken cancellationToken = default)
    {
        path = ResolvePath(path);
        return File.Exists(path)
            ? Task.FromResult(true)
            : DownloadOodleTextureDllFromOodleUEAsync(HttpUtils.DownloadClient, path, cancellationToken);
    }

    public static async Task<bool> DownloadOodleTextureDllFromOodleUEAsync(HttpClient client, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.GetAsync(TEXRT_URL, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var fs = File.Create(path);
            await responseStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Uncaught exception while downloading Oodle Texture Runtime");
        }

        return false;
    }

    private static void InitializeFromPath(string path)
    {
        try
        {
            _handle = NativeLibrary.Load(path);
            _bc7PrepReadHeader = Marshal.GetDelegateForFunctionPointer<OodleTexRtBc7PrepReadHeader>(
                NativeLibrary.GetExport(_handle, "OodleTexRT_BC7Prep_ReadHeader"));
            _bc7PrepMinDecodeScratchSize = Marshal.GetDelegateForFunctionPointer<OodleTexRtBc7PrepMinDecodeScratchSize>(
                NativeLibrary.GetExport(_handle, "OodleTexRT_BC7Prep_MinDecodeScratchSize"));
            _bc7PrepDecode = Marshal.GetDelegateForFunctionPointer<OodleTexRtBc7PrepDecode>(
                NativeLibrary.GetExport(_handle, "OodleTexRT_BC7Prep_Decode"));
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to load Oodle Texture Runtime from {Path}", path);
            _bc7PrepDecode = null;
            _bc7PrepReadHeader = null;
            _bc7PrepMinDecodeScratchSize = null;
        }
    }

    private static string ResolvePath(string? path) =>
        Path.GetFullPath(!string.IsNullOrWhiteSpace(path) ? path : OODLE_TEXRT_NAME);
}
