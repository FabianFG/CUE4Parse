using System.Collections.Frozen;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse.FileProvider.Objects;

public abstract class GameFile
{
    private static readonly ILogger Log = Serilog.Log.ForContext<GameFile>();
    
    public static readonly string[] UePackageExtensions = ["uasset", "umap"];
    public static readonly string[] UePackagePayloadExtensions = ["uexp", "ubulk", "uptnl"];
    public static readonly string[] UeKnownExtensions =
    [
        ..UePackageExtensions, ..UePackagePayloadExtensions,
        "bin", "ini", "uplugin", "upluginmanifest", "locres", "locmeta",
        "wem", "bnk", "pck", "bank", "awb", "acb"
    ];

    // Immutable lookup tables optimized once during startup.
    public static readonly FrozenSet<string> UePackageExtensionsSet = UePackageExtensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public static readonly FrozenSet<string> UePackagePayloadExtensionsSet = UePackagePayloadExtensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public static readonly FrozenSet<string> UeKnownExtensionsSet = UeKnownExtensions.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    // Avoid retaining duplicate extension and directory strings for every file.
    private static readonly ConcurrentDictionary<string, string> _internedExtensions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> _internedDirectories = new(StringComparer.Ordinal);

    private string _path;
    private string? _directory;
    private string? _pathWithoutExtension;
    private string? _name;
    private string? _nameWithoutExtension;
    private string? _extension;

    protected GameFile() { }
    protected GameFile(string path, long size)
    {
        Path = path;
        Size = size;
    }

    public abstract bool IsEncrypted { get; }
    public abstract CompressionMethod CompressionMethod { get; }

    public string Path
    {
        get => _path;
        protected internal set
        {
            _path = value;

            _directory = null;
            _pathWithoutExtension = null;
            _name = null;
            _nameWithoutExtension = null;
            _extension = null;
        }
    }
    public long Size { get; protected init; }

    public string Directory => _directory ??= Intern(_internedDirectories, Path.SubstringBeforeLast('/'));
    public string PathWithoutExtension => _pathWithoutExtension ??= Path.SubstringBeforeLast('.');
    public string Name => _name ??= Path.SubstringAfterLast('/');
    public string NameWithoutExtension
    {
        get
        {
            if (_nameWithoutExtension is not null) return _nameWithoutExtension;

            var nameStart = Path.LastIndexOf('/') + 1;
            var extensionSeparator = Path.LastIndexOf('.');
            return _nameWithoutExtension = extensionSeparator < nameStart
                ? Name
                : Path.Substring(nameStart, extensionSeparator - nameStart);
        }
    }
    public string Extension => _extension ??= Intern(_internedExtensions, Name.SubstringAfterLast('.'));

    public bool IsUePackage => UePackageExtensionsSet.Contains(Extension);
    public bool IsUePackagePayload => UePackagePayloadExtensionsSet.Contains(Extension);

    public abstract byte[] Read(FByteBulkDataHeader? header = null);
    public abstract FArchive CreateReader(FByteBulkDataHeader? header = null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead([MaybeNullWhen(false)] out byte[] data, FByteBulkDataHeader? header = null)
    {
        try
        {
            data = Read(header);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Could not read GameFile {this}");
            data = null;
        }
        return data != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCreateReader([MaybeNullWhen(false)] out FArchive reader, FByteBulkDataHeader? header = null)
    {
        try
        {
            reader = CreateReader(header);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Could not create reader for GameFile {this}");
            reader = null;
        }
        return reader != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[]? SafeRead(FByteBulkDataHeader? header = null)
    {
        TryRead(out var data, header);
        return data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FArchive? SafeCreateReader(FByteBulkDataHeader? header = null)
    {
        TryCreateReader(out var reader, header);
        return reader;
    }

    // No ConfigureAwait(false) here since the context is needed handling exceptions

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<byte[]> ReadAsync() => await Task.Run(() => Read());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<FArchive> CreateReaderAsync() => await Task.Run(() => CreateReader());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<byte[]?> SafeReadAsync() => await Task.Run(() => SafeRead());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<FArchive?> SafeCreateReaderAsync() => await Task.Run(() => SafeCreateReader());

    public override string ToString() => Path;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string Intern(ConcurrentDictionary<string, string> pool, string value) =>
        pool.GetOrAdd(value, static candidate => candidate);
}
