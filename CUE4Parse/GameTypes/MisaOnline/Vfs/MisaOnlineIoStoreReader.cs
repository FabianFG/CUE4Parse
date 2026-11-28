using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.MisaOnline.Vfs;

internal sealed class MisaOnlineIoStoreReader(FArchive tocStream, Func<string, FArchive> openContainerStreamFunc)
    : IoStoreReader(tocStream, openContainerStreamFunc)
{
    private IReadOnlyDictionary<FPackageId, string>? _packagePaths;

    public override bool HasDirectoryIndex => base.HasDirectoryIndex || _packagePaths is not null;

    internal void SetPackagePaths(IReadOnlyDictionary<FPackageId, string> packagePaths) => _packagePaths = packagePaths;

    public override void Mount(StringComparer pathComparer)
    {
        if (base.HasDirectoryIndex)
        {
            base.Mount(pathComparer);
            return;
        }

        var packagePaths = _packagePaths ?? throw new InvalidOperationException("Package paths are required to mount this IoStore.");
        MountPoint = "NonIndexed/";
        PackageIdIndex.Clear();
        OptionalSegmentPackageIdIndex.Clear();
        var files = new Dictionary<string, GameFile>(TocResource.ChunkIds.Length, pathComparer);

        for (uint i = 0; i < TocResource.ChunkIds.Length; i++)
        {
            ref readonly var chunkId = ref TocResource.ChunkIds[i];
            if (chunkId.ChunkType == (byte) EIoChunkType5.ContainerHeader)
                continue;

            FIoStoreEntry entry;
            if (chunkId.ChunkType is >= (byte) EIoChunkType5.ExportBundleData and <= (byte) EIoChunkType5.MemoryMappedBulkData &&
                packagePaths.TryGetValue(chunkId.AsPackageId(), out var packagePath))
            {
                var extensionStart = packagePath.LastIndexOf('.');
                var extension = chunkId.ChunkType == (byte) EIoChunkType5.ExportBundleData
                    ? packagePath[(extensionStart + 1)..]
                    : chunkId.GetExtension(this);
                var path = packagePath[..extensionStart] + (chunkId._chunkIndex == 0 ? "." : ".o.") + extension;
                entry = new FIoStoreEntry(this, path, i);
            }
            else
            {
                entry = new FIoStoreEntry(this, i);
            }

            if (entry.IsPackageData)
            {
                if (chunkId._chunkIndex == 0)
                    PackageIdIndex[chunkId.AsPackageId()] = entry;
                else
                    OptionalSegmentPackageIdIndex[chunkId.AsPackageId()] = entry;
            }

            files[entry.Path] = entry;
        }

        Files = files;
        EncryptedFileCount = IsEncrypted ? files.Count : 0;
        InitializeContainerHeader();
    }
}
