using CUE4Parse.FileProvider.Objects;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider;

internal sealed class PackageCache(AbstractFileProvider provider)
{
    private const long MaxPackageBytes = 512 * 1024 * 1024;
    private const long MinEntryBytes = 16 * 1024;

    private readonly Lock _lock = new();
    private readonly Dictionary<GameFile, LinkedListNode<Entry>> _packages = new(ReferenceEqualityComparer.Instance);
    private readonly LinkedList<Entry> _recency = new();
    private long _packageBytes;
    private LoadState _state;

    private readonly struct Entry(GameFile file, IPackage package, long estimatedBytes)
    {
        public readonly GameFile File = file;
        public readonly IPackage Package = package;
        public readonly long EstimatedBytes = estimatedBytes;
    }

    internal readonly record struct LoadState(
        long FileIndexRevision,
        TypeMappings? Mappings,
        EGame Game,
        FPackageFileVersion Version,
        EUnrealEngineObjectLicenseeUEVersion LicenseeVersion,
        ETexturePlatform Platform,
        bool ReadScriptData,
        bool ReadShaderMaps,
        bool ReadNaniteData,
        bool SkipReferencedTextures,
        bool UseLazyPackageSerialization)
    {
        public LoadState(AbstractFileProvider provider) : this(
            provider.Files.Revision,
            provider.MappingsForGame,
            provider.Versions.Game,
            provider.Versions.Ver,
            provider.Versions.LicenseeVer,
            provider.Versions.Platform,
            provider.ReadScriptData,
            provider.ReadShaderMaps,
            provider.ReadNaniteData,
            provider.SkipReferencedTextures,
            provider.UseLazyPackageSerialization)
        {
        }
    }

    public IPackage? Get(GameFile file, out LoadState state)
    {
        lock (_lock)
        {
            state = new LoadState(provider);
            if (state != _state)
            {
                _packages.Clear();
                _recency.Clear();
                _packageBytes = 0;
                _state = state;
            }

            if (!_packages.TryGetValue(file, out var node))
                return null;

            _recency.Remove(node);
            _recency.AddLast(node);
            return node.Value.Package;
        }
    }

    public IPackage Add(GameFile file, IPackage package, long exportBytes, LoadState state)
    {
        var estimatedBytes = Math.Max(MinEntryBytes, file.Size + exportBytes);
        if (estimatedBytes > MaxPackageBytes)
            return package;

        lock (_lock)
        {
            if (state != _state || state != new LoadState(provider))
                return package;
            if (_packages.TryGetValue(file, out var existing))
                return existing.Value.Package;

            while (_packageBytes + estimatedBytes > MaxPackageBytes)
            {
                var oldest = _recency.First!.Value;
                _packageBytes -= oldest.EstimatedBytes;
                _packages.Remove(oldest.File);
                _recency.RemoveFirst();
            }

            _packages.Add(file, _recency.AddLast(new Entry(file, package, estimatedBytes)));
            _packageBytes += estimatedBytes;
            return package;
        }
    }
}
