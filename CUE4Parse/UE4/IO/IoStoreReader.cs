using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO
{
    public class IoStoreReader : AbstractAesVfsReader
    {
        public readonly IReadOnlyList<FArchive> ContainerStreams;

        public readonly FIoStoreTocResource TocResource;
        public readonly Dictionary<FIoChunkId, FIoOffsetAndLength>? TocImperfectHashMapFallback;
        public readonly FIoStoreTocHeader Info;
        public FIoContainerHeader? ContainerHeader { get; private set; }
        public override string MountPoint { get; protected set; }
        public override FGuid EncryptionKeyGuid => Info.EncryptionKeyGuid;
        public sealed override long Length { get; set; }
        public override bool IsEncrypted => Info.ContainerFlags.HasFlag(EIoContainerFlags.Encrypted);
        public override bool HasDirectoryIndex => TocResource.DirectoryIndexBuffer != null;


        public IoStoreReader(string tocPath, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, VersionContainer? versions = null)
            : this(new FileInfo(tocPath), readOptions, versions) { }
        public IoStoreReader(FileInfo utocFile, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, VersionContainer? versions = null)
            : this(new FByteArchive(utocFile.FullName, File.ReadAllBytes(utocFile.FullName), versions),
                it => new FStreamArchive(it, File.Open(it, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), versions), readOptions) { }
        public IoStoreReader(string tocPath, Stream tocStream, Stream casStream, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, VersionContainer? versions = null)
            : this(new FStreamArchive(tocPath, tocStream, versions),
                it => new FStreamArchive(it, casStream, versions), readOptions) { }
        public IoStoreReader(string tocPath, Stream tocStream, Func<string, FArchive> openContainerStreamFunc, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, VersionContainer? versions = null)
            : this(new FStreamArchive(tocPath, tocStream, versions), openContainerStreamFunc, readOptions) { }

        public IoStoreReader(FArchive tocStream, Func<string, FArchive> openContainerStreamFunc, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex)
            : base(tocStream.Name, tocStream.Versions)
        {
            Length = tocStream.Length;
            TocResource = new FIoStoreTocResource(tocStream, readOptions);

            List<FArchive> containerStreams;
            if (TocResource.Header.PartitionCount <= 1)
            {
                containerStreams = new List<FArchive>(1);
                try
                {
                    containerStreams.Add(openContainerStreamFunc(tocStream.Name.SubstringBeforeLast('.') + ".ucas"));
                }
                catch (Exception e)
                {
                    throw new FIoStatusException(EIoErrorCode.FileOpenFailed, $"Failed to open container partition 0 for {tocStream.Name}", e);
                }
            }
            else
            {
                containerStreams = new List<FArchive>((int) TocResource.Header.PartitionCount);
                var environmentPath = tocStream.Name.SubstringBeforeLast('.');
                for (int i = 0; i < TocResource.Header.PartitionCount; i++)
                {
                    try
                    {
                        var path = i > 0 ? string.Concat(environmentPath, "_s", i, ".ucas") : string.Concat(environmentPath, ".ucas");
                        containerStreams.Add(openContainerStreamFunc(path));
                    }
                    catch (Exception e)
                    {
                        throw new FIoStatusException(EIoErrorCode.FileOpenFailed, $"Failed to open container partition {i} for {tocStream.Name}", e);
                    }
                }
            }

            Length += containerStreams.Sum(x => x.Length);
            ContainerStreams = containerStreams;
            if (TocResource.ChunkPerfectHashSeeds != null)
            {
                TocImperfectHashMapFallback = new();
                if (TocResource.ChunkIndicesWithoutPerfectHash != null)
                {
                    foreach (var chunkIndexWithoutPerfectHash in TocResource.ChunkIndicesWithoutPerfectHash)
                    {
                        TocImperfectHashMapFallback[TocResource.ChunkIds[chunkIndexWithoutPerfectHash]] = TocResource.ChunkOffsetLengths[chunkIndexWithoutPerfectHash];
                    }
                }
            }
#if GENERATE_CHUNK_ID_DICT
            else
            {
                TocImperfectHashMapFallback = new Dictionary<FIoChunkId, FIoOffsetAndLength>((int) TocResource.Header.TocEntryCount);
                for (var i = 0; i < TocResource.ChunkIds.Length; i++)
                {
                    TocImperfectHashMapFallback[TocResource.ChunkIds[i]] = TocResource.ChunkOffsetLengths[i];
                }
            }
#endif
            Info = TocResource.Header;
            if (TocResource.Header.Version > EIoStoreTocVersion.Latest)
            {
                log.Warning("Io Store \"{0}\" has unsupported version {1}", Path, (int) Info.Version);
            }
        }

        public override byte[] Extract(VfsEntry entry)
        {
            if (!(entry is FIoStoreEntry ioEntry) || entry.Vfs != this) throw new ArgumentException($"Wrong io store reader, required {entry.Vfs.Path}, this is {Path}");
            return Read(ioEntry.Offset, ioEntry.Size);
        }

        // If anyone really comes to read this here are some of my thoughts on designing loading of chunk ids
        // UE Code builds a Map<FIoChunkId, FIoOffsetAndLength> to optimize loading of chunks just by their id
        // After some testing this appeared to take ~30mb of memory
        // We can save that memory since we rarely use loading by FIoChunkId directly (I'm pretty sure we just do for the global reader)
        // If anyone want to use the map anyway the define GENERATE_CHUNK_ID_DICT exists

        public bool DoesChunkExist(FIoChunkId chunkId) => TryResolve(chunkId, out _);

        public bool TryResolve(FIoChunkId chunkId, out FIoOffsetAndLength outOffsetLength)
        {
            if (TocResource.ChunkPerfectHashSeeds != null)
            {
                var chunkCount = TocResource.Header.TocEntryCount;
                if (chunkCount == 0)
                {
                    outOffsetLength = default;
                    return false;
                }
                var seedCount = (uint) TocResource.ChunkPerfectHashSeeds.Length;
                var seedIndex = (uint) (chunkId.HashWithSeed(0) % seedCount);
                var seed = TocResource.ChunkPerfectHashSeeds[seedIndex];
                if (seed == 0)
                {
                    outOffsetLength = default;
                    return false;
                }
                uint slot;
                if (seed < 0)
                {
                    var seedAsIndex = (uint) (-seed - 1);
                    if (seedAsIndex < chunkCount)
                    {
                        slot = seedAsIndex;
                    }
                    else
                    {
                        // Entry without perfect hash
                        return TryResolveImperfect(chunkId, out outOffsetLength);
                    }
                }
                else
                {
                    slot = (uint) (chunkId.HashWithSeed(seed) % chunkCount);
                }
                if (TocResource.ChunkIds[slot].GetHashCode() == chunkId.GetHashCode())
                {
                    outOffsetLength = TocResource.ChunkOffsetLengths[slot];
                    return true;
                }
                outOffsetLength = default;
                return false;
            }

            return TryResolveImperfect(chunkId, out outOffsetLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryResolveImperfect(FIoChunkId chunkId, out FIoOffsetAndLength outOffsetLength)
        {
            if (TocImperfectHashMapFallback != null)
            {
                return TocImperfectHashMapFallback.TryGetValue(chunkId, out outOffsetLength);
            }

            var chunkIndex = Array.IndexOf(TocResource.ChunkIds, chunkId);
            if (chunkIndex == -1)
            {
                outOffsetLength = default;
                return false;
            }

            outOffsetLength = TocResource.ChunkOffsetLengths[chunkIndex];
            return true;
        }

        public byte[] Read(FIoChunkId chunkId)
        {
            if (TryResolve(chunkId, out var offsetLength))
            {
                return Read((long) offsetLength.Offset, (long) offsetLength.Length);
            }

            throw new KeyNotFoundException($"Couldn't find chunk {chunkId} in IoStore {Name}");
        }

        private byte[] Read(long offset, long length)
        {
            var compressionBlockSize = TocResource.Header.CompressionBlockSize;
            var dst = new byte[length];
            var firstBlockIndex = (int) (offset / compressionBlockSize);
            var lastBlockIndex = (int) (((offset + dst.Length).Align((int) compressionBlockSize) - 1) / compressionBlockSize);
            var offsetInBlock = offset % compressionBlockSize;
            var remainingSize = length;
            var dstOffset = 0;

            var compressedBuffer = Array.Empty<byte>();
            var uncompressedBuffer = Array.Empty<byte>();

            var clonedReaders = new FArchive?[ContainerStreams.Count];

            for (int blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
            {
                ref var compressionBlock = ref TocResource.CompressionBlocks[blockIndex];

                var rawSize = compressionBlock.CompressedSize.Align(Aes.ALIGN);
                if (compressedBuffer.Length < rawSize)
                {
                    //Console.WriteLine($"{chunkId}: block {blockIndex} CompressedBuffer size: {rawSize} - Had to create copy");
                    compressedBuffer = new byte[rawSize];
                }

                var uncompressedSize = compressionBlock.UncompressedSize;
                if (uncompressedBuffer.Length < uncompressedSize)
                {
                    //Console.WriteLine($"{chunkId}: block {blockIndex} UncompressedBuffer size: {uncompressedSize} - Had to create copy");
                    uncompressedBuffer = new byte[uncompressedSize];
                }

                var partitionIndex = (int) ((ulong) compressionBlock.Offset / TocResource.Header.PartitionSize);
                var partitionOffset = (long) ((ulong) compressionBlock.Offset % TocResource.Header.PartitionSize);
                FArchive reader;
                if (IsConcurrent)
                {
                    ref var clone = ref clonedReaders[partitionIndex];
                    clone ??= (FArchive) ContainerStreams[partitionIndex].Clone();
                    reader = clone;
                }
                else reader = ContainerStreams[partitionIndex];

                reader.Position = partitionOffset;
                reader.Read(compressedBuffer, 0, (int) rawSize);
                compressedBuffer = DecryptIfEncrypted(compressedBuffer, 0, (int) rawSize);

                byte[] src;
                if (compressionBlock.CompressionMethodIndex == 0)
                {
                    src = compressedBuffer;
                }
                else
                {
                    var compressionMethod = TocResource.CompressionMethods[compressionBlock.CompressionMethodIndex];
                    Compression.Compression.Decompress(compressedBuffer, 0, (int) rawSize, uncompressedBuffer, 0,
                        (int) uncompressedSize, compressionMethod, reader);
                    src = uncompressedBuffer;
                }

                var sizeInBlock = (int) Math.Min(compressionBlockSize - offsetInBlock, remainingSize);
                Buffer.BlockCopy(src, (int) offsetInBlock, dst, dstOffset, sizeInBlock);
                offsetInBlock = 0;
                remainingSize -= sizeInBlock;
                dstOffset += sizeInBlock;

                reader.Position = 0;
            }

            return dst;
        }

        public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
        {
            var watch = new Stopwatch();
            watch.Start();

            ProcessIndex(caseInsensitive);
            if (Game >= EGame.GAME_UE5_0) // We can safely skip reading container header on UE4
            {
                ContainerHeader = ReadContainerHeader();
            }

            if (Globals.LogVfsMounts)
            {
                var elapsed = watch.Elapsed;
                var sb = new StringBuilder($"IoStore \"{Name}\": {FileCount} files");
                if (EncryptedFileCount > 0)
                    sb.Append($" ({EncryptedFileCount} encrypted)");
                if (MountPoint.Contains("/"))
                    sb.Append($", mount point: \"{MountPoint}\"");
                sb.Append($", version {(int) Info.Version} in {elapsed}");
                log.Information(sb.ToString());
            }

            return Files;
        }

        private void ProcessIndex(bool caseInsensitive)
        {
            if (!HasDirectoryIndex || TocResource.DirectoryIndexBuffer == null) throw new ParserException("No directory index");
            var directoryIndex = new FByteArchive(Path, DecryptIfEncrypted(TocResource.DirectoryIndexBuffer));

            string mountPoint;
            try
            {
                mountPoint = directoryIndex.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{Path}'", e);
            }

            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            var directoryEntries = directoryIndex.ReadArray<FIoDirectoryIndexEntry>();
            var fileEntries = directoryIndex.ReadArray<FIoFileIndexEntry>();
            var stringTable = directoryIndex.ReadArray(directoryIndex.ReadFString);

            var files = new Dictionary<string, GameFile>(fileEntries.Length);
            ReadIndex(MountPoint, 0U);

            void ReadIndex(string directoryName, uint dir)
            {
                const uint invalidHandle = uint.MaxValue;

                while (dir != invalidHandle)
                {
                    ref var dirEntry = ref directoryEntries[dir];
                    var subDirectoryName = dirEntry.Name == invalidHandle ? directoryName : $"{directoryName}{stringTable[dirEntry.Name]}/";

                    var file = dirEntry.FirstFileEntry;
                    while (file != invalidHandle)
                    {
                        ref var fileEntry = ref fileEntries[file];

                        var path = string.Concat(subDirectoryName, stringTable[fileEntry.Name]);
                        var entry = new FIoStoreEntry(this, path, fileEntry.UserData);
                        if (entry.IsEncrypted)
                            EncryptedFileCount++;
                        if (caseInsensitive)
                            files[path.ToLowerInvariant()] = entry;
                        else
                            files[path] = entry;

                        file = fileEntry.NextFileEntry;
                    }

                    ReadIndex(subDirectoryName, dirEntry.FirstChildEntry);
                    dir = dirEntry.NextSiblingEntry;
                }
            }

            Files = files;
        }

        private FIoContainerHeader ReadContainerHeader()
        {
            var headerChunkId = new FIoChunkId(TocResource.Header.ContainerId.Id, 0, Game >= EGame.GAME_UE5_0 ? (byte) EIoChunkType5.ContainerHeader : (byte) EIoChunkType.ContainerHeader);
            var Ar = new FByteArchive("ContainerHeader", Read(headerChunkId), Versions);
            return new FIoContainerHeader(Ar);
        }

        public override byte[] MountPointCheckBytes() => TocResource.DirectoryIndexBuffer ?? new byte[MAX_MOUNTPOINT_TEST_LENGTH];
        protected override byte[] ReadAndDecrypt(int length) => throw new InvalidOperationException("Io Store can't read bytes without context"); //ReadAndDecrypt(length, Ar, IsEncrypted);

        public override void Dispose()
        {
            foreach (var stream in ContainerStreams)
            {
                stream.Dispose();
            }
        }
    }
}
