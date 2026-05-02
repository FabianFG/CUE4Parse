using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.UE4.Lua.unluac;

public static class UnluacHelper
{
    public const uint LuaMagic = 0x61754c1B;
    private const string _currentVersion = "1.0.0";

    public static readonly string DOWNLOAD_URL = $"https://github.com/FModel/unluac/releases/download/v{_currentVersion}/unluac.dll.gz";
    public static readonly string DOWNLOAD_URL_LINUX = $"https://github.com/FModel/unluac/releases/download/v{_currentVersion}/unluac.so.gz";
    public const string DLL_NAME = "unluac.dll";
    public static readonly string DLL_NAME_LINUX = $"unluac-{_currentVersion}.so";

    public static Unluac? Instance { get; private set; }
    public static string DllName => OperatingSystem.IsLinux() ? DLL_NAME_LINUX : DLL_NAME;

    public static EUnluacErrorCode Decompile(byte[] lua, byte[] opmap, uint flags, out byte[] output, out byte[] log)
    {
        var instance = Instance;
        if (instance is null)
        {
            throw new ParserException("unluac decompile failed: not initialized");
        }

        return instance.Decompile(lua, opmap, flags, out output, out log);
    }

    public static void Initialize(Unluac instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static void Initialize(string? path = null) => InitializeAsync(path).GetAwaiter().GetResult();

    public static async Task InitializeAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        if (Instance is not null) return;

        var dllPath = string.IsNullOrWhiteSpace(path) ? DllName : path;
        if (!await DownloadDllAsync(dllPath, null, cancellationToken).ConfigureAwait(false))
        {
            Log.Warning("Unable to download unluac dll");
            return;
        }

        Initialize(new Unluac(dllPath));
    }

    public static bool DownloadDll(string? path = null, string? url = null) => DownloadDllAsync(path, url).GetAwaiter().GetResult();

    public static Task<bool> DownloadDllAsync(string? path, string? url = null) => DownloadDllAsync(path, url, CancellationToken.None);

    public static async Task<bool> DownloadDllAsync(string? path, string? url, CancellationToken cancellationToken)
    {
        var dllPath = string.IsNullOrWhiteSpace(path) ? DllName : path;
        if (!RequiresUpdate(dllPath)) return true;

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

            Log.Information("Successfully downloaded unluac dll at {0}", dllPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Uncaught exception while downloading unluac dll");
        }
        return false;
    }

    private static string ResolveDownloadUrl(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url)) return url;
        return OperatingSystem.IsLinux() ? DOWNLOAD_URL_LINUX : DOWNLOAD_URL;
    }

    private static Version GetDllFileVersion(string path)
    {
        var info = FileVersionInfo.GetVersionInfo(path);
        return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
    }

    private static bool RequiresUpdate(string dllPath)
    {
        if (!File.Exists(dllPath)) return true;
        if (OperatingSystem.IsLinux()) return false;
        return !Version.TryParse(_currentVersion, out var currentVersion) || GetDllFileVersion(dllPath) < currentVersion;
    }
}
