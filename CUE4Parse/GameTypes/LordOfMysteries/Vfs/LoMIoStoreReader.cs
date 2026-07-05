using System.Diagnostics;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.GameTypes.LordOfMysteries.Vfs;

public sealed class LoMIoStoreReader(LoMIoStoreManifest manifest, LoMDirectoryIndex directoryIndex, VersionContainer versions) : IoStoreReader(manifest.TocArchive, path => new FRandomAccessFileStreamArchive(path, versions))
{
    private readonly LoMDirectoryIndex _directoryIndex = directoryIndex;

    public override bool HasDirectoryIndex => true;

    public override void Mount(StringComparer pathComparer)
    {
        if (TocResource.DirectoryIndexBufferOffset != -1)
        {
            base.Mount(pathComparer);
            return;
        }

        var watch = Stopwatch.StartNew();
        MountPoint = "NonIndexed/";

        var files = new Dictionary<string, GameFile>(TocResource.ChunkIds.Length, pathComparer);
        for (uint i = 0; i < TocResource.ChunkIds.Length; i++)
        {
            var chunkId = TocResource.ChunkIds[i];
            FIoStoreEntry entry;
            if (chunkId.ChunkType is (byte) EIoChunkType5.ContainerHeader or (byte) EIoChunkType5.ShaderCode) continue;
            if (_directoryIndex.TryGetPackagePath(chunkId, out var path))
            {
                entry = new FIoStoreEntry(this, path, i);
            }
            else
            {
                entry = new FIoStoreEntry(this, i);
            }

            if (entry.IsEncrypted) EncryptedFileCount++;
            if (entry.IsUePackage) PackageIdIndex[entry.ChunkId.AsPackageId()] = entry;
            files[entry.Path] = entry;
        }

        Files = files;
        InitializeContainerHeader();

        if (Globals.LogVfsMounts)
        {
            var sb = new StringBuilder($"IoStore \"{Name}\": {FileCount} files");
            if (EncryptedFileCount > 0)
                sb.Append($" ({EncryptedFileCount} encrypted)");
            sb.Append($", mount point: \"{MountPoint}\"");
            sb.Append($", order {ReadOrder}");
            sb.Append($", version {(int) TocResource.Header.Version} in {watch.Elapsed}");
            Log.Information(sb.ToString());
        }
    }
}
