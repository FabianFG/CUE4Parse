using System;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using Serilog;

namespace CUE4Parse.UE4.IO.Objects
{
    [Flags]
    public enum EIoStoreTocReadOptions
    {
        Default,
        ReadDirectoryIndex	= 1 << 0,
        ReadTocMeta			= 1 << 1,
        ReadAll				= ReadDirectoryIndex | ReadTocMeta
    }

    public class FIoStoreTocResource
    {
        public readonly FIoStoreTocHeader Header;
        public readonly FIoChunkId[] ChunkIds;
        public readonly FIoOffsetAndLength[] ChunkOffsetLengths;
        public readonly int[]? ChunkPerfectHashSeeds;
        public readonly int[]? ChunkIndicesWithoutPerfectHash;
        public readonly FIoStoreTocCompressedBlockEntry[] CompressionBlocks;
        public readonly CompressionMethod[] CompressionMethods;

        public readonly byte[]? DirectoryIndexBuffer;
        public readonly FIoStoreTocEntryMeta[]? ChunkMetas;

        public FIoStoreTocResource(FArchive Ar, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.Default)
        {
            var streamBuffer = new byte[Ar.Length];
            Ar.Read(streamBuffer, 0, streamBuffer.Length);
            using var archive = new FByteArchive(Ar.Name, streamBuffer, Ar.Versions);

            // Header
            Header = new FIoStoreTocHeader(archive);

            if (Header.Version < EIoStoreTocVersion.PartitionSize)
            {
                Header.PartitionCount = 1;
                Header.PartitionSize = ulong.MaxValue;
            }

            // Chunk IDs
            ChunkIds = archive.ReadArray<FIoChunkId>((int) Header.TocEntryCount);

            // Chunk offsets
            ChunkOffsetLengths = new FIoOffsetAndLength[Header.TocEntryCount];
            for (int i = 0; i < Header.TocEntryCount; i++)
            {
                ChunkOffsetLengths[i] = new FIoOffsetAndLength(archive);
            }

            // Chunk perfect hash map
            uint perfectHashSeedsCount = 0;
            uint chunksWithoutPerfectHashCount = 0;
            if (Header.Version >= EIoStoreTocVersion.PerfectHashWithOverflow)
            {
                perfectHashSeedsCount = Header.TocChunkPerfectHashSeedsCount;
                chunksWithoutPerfectHashCount = Header.TocChunksWithoutPerfectHashCount;
            }
            else if (Header.Version >= EIoStoreTocVersion.PerfectHash)
            {
                perfectHashSeedsCount = Header.TocChunkPerfectHashSeedsCount;
            }
            if (perfectHashSeedsCount > 0)
            {
                ChunkPerfectHashSeeds = archive.ReadArray<int>((int) perfectHashSeedsCount);
            }
            if (chunksWithoutPerfectHashCount > 0)
            {
                ChunkIndicesWithoutPerfectHash = archive.ReadArray<int>((int) chunksWithoutPerfectHashCount);
            }

            // Compression blocks
            CompressionBlocks = new FIoStoreTocCompressedBlockEntry[Header.TocCompressedBlockEntryCount];
            for (int i = 0; i < Header.TocCompressedBlockEntryCount; i++)
            {
                CompressionBlocks[i] = new FIoStoreTocCompressedBlockEntry(archive);
            }

            // Compression methods
            unsafe
            {
                var bufferSize = (int) (Header.CompressionMethodNameLength * Header.CompressionMethodNameCount);
                var buffer = stackalloc byte[bufferSize];
                archive.Serialize(buffer, bufferSize);
                CompressionMethods = new CompressionMethod[Header.CompressionMethodNameCount + 1];
                CompressionMethods[0] = CompressionMethod.None;
                for (var i = 0; i < Header.CompressionMethodNameCount; i++)
                {
                    var name = new string((sbyte*) buffer + i * Header.CompressionMethodNameLength, 0, (int) Header.CompressionMethodNameLength).TrimEnd('\0');
                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (!Enum.TryParse(name, true, out CompressionMethod method))
                    {
                        Log.Warning($"Unknown compression method '{name}' in {Ar.Name}");
                        method = CompressionMethod.Unknown;
                    }

                    CompressionMethods[i + 1] = method;
                }
            }

            // Chunk block signatures
            if (Header.ContainerFlags.HasFlag(EIoContainerFlags.Signed))
            {
                var hashSize = archive.Read<int>();
                // tocSignature and blockSignature both byte[hashSize]
                // and ChunkBlockSignature of FSHAHash[Header.TocCompressedBlockEntryCount]
                archive.Position += hashSize + hashSize + FSHAHash.SIZE * Header.TocCompressedBlockEntryCount;

                // You could verify hashes here but nah
            }

            // Directory index
            if (Header.Version >= EIoStoreTocVersion.DirectoryIndex &&
                Header.ContainerFlags.HasFlag(EIoContainerFlags.Indexed) &&
                Header.DirectoryIndexSize > 0)
            {
                if (readOptions.HasFlag(EIoStoreTocReadOptions.ReadDirectoryIndex))
                    DirectoryIndexBuffer = archive.ReadBytes((int) Header.DirectoryIndexSize);
                else
                    archive.Position += Header.DirectoryIndexSize;
            }

            // Meta
            if (readOptions.HasFlag(EIoStoreTocReadOptions.ReadTocMeta))
            {
                var replacedIoChunkHashWithIoHash = Header.Version >= EIoStoreTocVersion.ReplaceIoChunkHashWithIoHash;
                ChunkMetas = new FIoStoreTocEntryMeta[Header.TocEntryCount];
                for (int i = 0; i < Header.TocEntryCount; i++)
                {
                    ChunkMetas[i] = new FIoStoreTocEntryMeta(archive, replacedIoChunkHashWithIoHash);
                }

                // OnDemand
                if (Header.Version == EIoStoreTocVersion.OnDemandMetaData && Header.ContainerFlags.HasFlag(EIoContainerFlags.OnDemand))
                {
                    // FIoStoreTocOnDemandChunkMeta (FIoHash) OnDemandChunkMeta;
                    Ar.Position += Header.TocEntryCount * FSHAHash.SIZE;
                    // FIoStoreTocOnDemandCompressedBlockMeta (FIoHash) OnDemandCompressedBlockMeta;
                    Ar.Position += Header.TocCompressedBlockEntryCount * FSHAHash.SIZE;
                }
            }
        }
    }
}
