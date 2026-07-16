using System.Text;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.GameTypes.LordOfMysteries;

public sealed class LoMIoStoreManifest
{
    private const ulong PartitionSize = 0x100000000;

    public readonly FByteArchive TocArchive;

    private LoMIoStoreManifest(FByteArchive tocArchive) => TocArchive = tocArchive;

    public static IReadOnlyList<LoMIoStoreManifest> Read(FileInfo manifestFile, VersionContainer versions)
    {
        var manifest = LoMManifest.Read(manifestFile, versions);
        var compressionBlockSize = manifest.CompressionBlockSize;
        var ownerGroups = GetContainerOwnerGroups(manifest.BaseDirectory, manifest.Paths, manifest.Entries);
        var manifests = new List<LoMIoStoreManifest>(ownerGroups.Count);

        foreach (var group in ownerGroups)
        {
            var tocName = Path.ChangeExtension(group.BasePath, ".utoc");
            if (File.Exists(tocName))
                continue; // Normal .utoc files might already exist

            var tocBytes = ToIoStoreTocBytes(manifest, compressionBlockSize, group.Owners);
            manifests.Add(new LoMIoStoreManifest(new FByteArchive(tocName, tocBytes, versions)));
        }

        return manifests;
    }

    private static byte[] ToIoStoreTocBytes(LoMManifest manifest, uint compressionBlockSize, IReadOnlyList<LoMContainerOwner> owners)
    {
        var selectedEntries = new List<LoMEntry>();
        var selectedBlocks = new List<LoMCompressionBlock>();
        var ownerPartitionIndices = owners.ToDictionary(x => x.Owner, x => x.PartitionIndex);

        foreach (var entry in manifest.Entries)
        {
            if (!ownerPartitionIndices.TryGetValue(entry.Owner, out var partitionIndex) || entry.CompressionBlocksCount <= 0)
                continue;

            var localBlockOffset = selectedBlocks.Count;
            for (var blockIndex = entry.CompressionBlocksOffset; blockIndex < entry.CompressionBlocksOffset + entry.CompressionBlocksCount; blockIndex++)
            {
                if ((uint) blockIndex >= (uint) manifest.CompressionBlocks.Length)
                    continue;

                selectedBlocks.Add(manifest.CompressionBlocks[blockIndex].WithBaseOffset((long) (PartitionSize * (ulong) partitionIndex) + entry.Offset));
            }

            selectedEntries.Add(entry with { CompressionBlocksOffset = localBlockOffset });
        }

        var containerId = selectedEntries.FirstOrDefault(x => x.Id.ChunkType == (byte) EIoChunkType5.ContainerHeader).Id.ChunkId;
        var isEncrypted = selectedEntries.Any(entry => entry.Type == ELoMFileType.AssetEncrComp);

        using var toc = new FArchiveWriter();
        var partitionCount = owners.Count == 0 ? 1 : owners.Max(x => x.PartitionIndex) + 1;

        WriteHeader(toc, selectedEntries.Count, selectedBlocks.Count, manifest.CompressionMethods, compressionBlockSize, containerId, partitionCount, isEncrypted);

        foreach (var entry in selectedEntries)
            Write(toc, entry.Id);

        foreach (var entry in selectedEntries)
        {
            var offset = (ulong) entry.CompressionBlocksOffset * compressionBlockSize;
            var length = (ulong) entry.UncompressedSize;
            toc.Write((byte) (offset >> 32));
            toc.Write((byte) (offset >> 24));
            toc.Write((byte) (offset >> 16));
            toc.Write((byte) (offset >> 8));
            toc.Write((byte) offset);
            toc.Write((byte) (length >> 32));
            toc.Write((byte) (length >> 24));
            toc.Write((byte) (length >> 16));
            toc.Write((byte) (length >> 8));
            toc.Write((byte) length);
        }

        foreach (var block in selectedBlocks)
            Write(toc, block);

        foreach (var method in manifest.CompressionMethods)
        {
            var methodBytes = Encoding.ASCII.GetBytes(method);
            var methodBytesLength = Math.Min(methodBytes.Length, 32);
            toc.Write(methodBytes.AsSpan(..methodBytesLength));
            for (var i = methodBytesLength; i < 32; i++)
            {
                toc.Write((byte) 0);
            }
        }

        return toc.GetBuffer();
    }

    private static List<LoMContainerOwnerGroup> GetContainerOwnerGroups(string baseDirectory, string[] names, LoMEntry[] entries)
    {
        var groups = new Dictionary<string, LoMContainerOwnerGroup>(StringComparer.OrdinalIgnoreCase);
        var ownerSet = new HashSet<int>();
        foreach (var entry in entries)
        {
            if (entry is not { CompressionBlocksCount: > 0, Owner: >= 0 } || entry.Owner >= names.Length)
                continue;
            if (!names[entry.Owner].EndsWith(".ucas", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!ownerSet.Add(entry.Owner))
                continue;

            var containerPath = names[entry.Owner].Replace('/', Path.DirectorySeparatorChar);
            if (!Path.IsPathRooted(containerPath))
                containerPath = Path.Combine(baseDirectory, containerPath);

            var basePath = GetPartitionBasePath(containerPath, out var partitionIndex);
            if (!groups.TryGetValue(basePath, out var group))
            {
                group = new LoMContainerOwnerGroup(basePath);
                groups[basePath] = group;
            }

            group.Owners.Add(new LoMContainerOwner(entry.Owner, partitionIndex));
        }

        foreach (var group in groups.Values)
            group.Owners.Sort((a, b) => a.PartitionIndex.CompareTo(b.PartitionIndex));

        return [.. groups.Values];
    }

    private static string GetPartitionBasePath(string path, out int partitionIndex)
    {
        partitionIndex = 0;
        var extension = Path.GetExtension(path);
        var withoutExtension = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFileNameWithoutExtension(path));
        var suffixIndex = withoutExtension.LastIndexOf("_s", StringComparison.OrdinalIgnoreCase);
        if (suffixIndex == -1 || suffixIndex + 2 >= withoutExtension.Length)
            return path;

        var suffix = withoutExtension[(suffixIndex + 2)..];
        if (!suffix.All(char.IsDigit) || !int.TryParse(suffix, out partitionIndex))
        {
            partitionIndex = 0;
            return path;
        }

        return withoutExtension[..suffixIndex] + extension;
    }

    private static void WriteHeader(FArchiveWriter toc, int entryCount, int compressionBlockCount, string[] compressionMethods, uint compressionBlockSize, ulong containerId, int partitionCount, bool isEncrypted)
    {
        toc.Write(FIoStoreTocHeader.TOC_MAGIC);
        toc.Write((byte) EIoStoreTocVersion.PartitionSize);
        toc.Write((byte) 0);
        toc.Write((ushort) 0);
        toc.Write((uint) FIoStoreTocHeader.SIZE);
        toc.Write((uint) entryCount);
        toc.Write((uint) compressionBlockCount);
        toc.Write((uint) FIoStoreTocCompressedBlockEntry.SIZE);
        toc.Write((uint) compressionMethods.Length);
        toc.Write(32);
        toc.Write(compressionBlockSize);
        toc.Write(0);
        toc.Write((uint) partitionCount);
        toc.Write(containerId);
        for (var i = 0; i < FGuid.Size; i++)
            toc.Write((byte) 0);
        var flags = EIoContainerFlags.Compressed;
        if (isEncrypted)
        {
            flags |= EIoContainerFlags.Encrypted;
        }
        toc.Write((uint) flags);
        toc.Write(0);
        toc.Write(PartitionSize);
        toc.Write(0);
        toc.Write(0);
        for (var i = 0; i < 5; i++)
            toc.Write(0ul);
    }

    private static void Write(FArchiveWriter toc, FIoChunkId chunkId)
    {
        toc.Write(chunkId.ChunkId);
        toc.Write(chunkId._chunkIndex);
        toc.Write(chunkId._padding);
        toc.Write(chunkId.ChunkType);
    }

    private static void Write(FArchiveWriter toc, LoMCompressionBlock block)
    {
        toc.Write((ulong) block.Offset | ((ulong) block.CompressedSize << 40));
        toc.Write((uint) block.UncompressedSize | ((uint) block.CompressionMethod << 24));
    }

    private sealed class LoMContainerOwnerGroup(string basePath)
    {
        public readonly string BasePath = basePath;
        public readonly List<LoMContainerOwner> Owners = [];
    }

    private readonly record struct LoMContainerOwner(int Owner, int PartitionIndex);
}
