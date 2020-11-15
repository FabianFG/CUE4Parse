using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Vfs;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO
{
    public class IoStoreReader : AbstractAesVfsReader
    {
        public readonly FArchive Ar;
        
        public readonly FIoStoreTocResource TocResource;
#if GENERATE_CHUNK_ID_DICT
        public readonly Dictionary<FIoChunkId, FIoOffsetAndLength> Toc;
#endif
        public readonly FIoStoreTocHeader Info;
        public string MountPoint { get; private set; }
        public override FGuid EncryptionKeyGuid => Info.EncryptionKeyGuid;
        public override bool IsEncrypted => Info.ContainerFlags.HasFlag(EIoContainerFlags.Encrypted);
        public bool HasDirectoryIndex => TocResource.DirectoryIndexBuffer != null;

        public IoStoreReader(FArchive containerStream, FArchive tocStream, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex) : 
            base(containerStream.Name, containerStream.Ver, containerStream.Game)
        {
            Ar = containerStream;
            TocResource = new FIoStoreTocResource(tocStream, readOptions);
#if GENERATE_CHUNK_ID_DICT
            Toc = new Dictionary<FIoChunkId, FIoOffsetAndLength>((int) TocResource.Header.TocEntryCount);
            for (var i = 0; i < TocResource.ChunkIds.Length; i++)
            {
                Toc[TocResource.ChunkIds[i]] = TocResource.ChunkOffsetLengths[i];
            }
#endif
            
            Info = TocResource.Header;
            if (TocResource.Header.Version > EIoStoreTocVersion.Latest)
            {
                log.Warning($"Io Store \"{Name}\" has unsupported version {(int) Info.Version}");
            }
        }
        
        public IoStoreReader(string containerPath, string tocPath, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : this(new FileInfo(containerPath), new FileInfo(tocPath), readOptions, ver, game) {}
        public IoStoreReader(FileInfo containerFile, FileInfo tocFile, EIoStoreTocReadOptions readOptions = EIoStoreTocReadOptions.ReadDirectoryIndex, UE4Version ver = UE4Version.VER_UE4_LATEST, EGame game = EGame.GAME_UE4_LATEST)
            : this(new FStreamArchive(containerFile.Name, containerFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ver, game), new FByteArchive(tocFile.Name, File.ReadAllBytes(tocFile.FullName), ver, game), readOptions) {}

        public override byte[] Extract(VfsEntry entry)
        {
            if (!(entry is FIoStoreEntry ioEntry) || entry.Vfs != this) throw new ArgumentException($"Wrong io store reader, required {entry.Vfs.Name}, this is {Name}");
            return Read(ioEntry.ChunkId, ioEntry.Offset, ioEntry.Size);
        }
        
        // If anyone really comes to read this here are some of my thoughts on designing loading of chunk ids
        // UE Code builds a Map<FIoChunkId, FIoOffsetAndLength> to optimize loading of chunks just by their id
        // After some testing this appeared to take ~30mb of memory
        // We can save that memory since we rarely use loading by FIoChunkId directly (I'm pretty sure we just do for the global reader)
        // If anyone want to use the map anyway the define GENERATE_CHUNK_ID_DICT exists
        

        public bool DoesChunkExist(FIoChunkId chunkId)
        {
#if GENERATE_CHUNK_ID_DICT
            return Toc.ContainsKey(chunkId);      
#else
            return Array.IndexOf(TocResource.ChunkIds, chunkId) >= 0;          
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ChunkIndex(FIoChunkId chunkId) => Array.IndexOf(TocResource.ChunkIds, chunkId);

        public byte[] Read(FIoChunkId chunkId)
        {
#if GENERATE_CHUNK_ID_DICT
            var offsetLength = Toc[chunkId];
#else
            var offsetLength = TocResource.ChunkOffsetLengths[Array.IndexOf(TocResource.ChunkIds, chunkId)];
#endif
            return Read(chunkId, (long) offsetLength.Offset, (long) offsetLength.Length);
        }
        
        private byte[] Read(FIoChunkId chunkId, long offset, long length)
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

            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;

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

                reader.Position = compressionBlock.Offset;
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
            }

            return dst;
        }

        public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
        {
            var watch = new Stopwatch();
            watch.Start();
            
            ProcessIndex(caseInsensitive);
            
            var elapsed = watch.Elapsed;
            var sb = new StringBuilder($"IoStore {Name}: {FileCount} files");
            if (EncryptedFileCount != 0)
                sb.Append($" ({EncryptedFileCount} encrypted)");
            if (MountPoint.Contains("/"))
                sb.Append($", mount point: \"{MountPoint}\"");
            sb.Append($", version {(int) Info.Version} in {elapsed}");
            log.Information(sb.ToString());
            return Files;
        }
        public IReadOnlyDictionary<string, GameFile> ProcessIndex(bool caseInsensitive)
        {
            if (!HasDirectoryIndex || TocResource.DirectoryIndexBuffer == null) throw new ParserException(Ar, "No directory index");
            var directoryIndex = new FByteArchive(Name, DecryptIfEncrypted(TocResource.DirectoryIndexBuffer));
            
            string mountPoint;
            try
            {
                mountPoint = directoryIndex.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{Name}'", e);
            }
            
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            var directoryEntries = directoryIndex.ReadArray<FIoDirectoryIndexEntry>();
            var fileEntries = directoryIndex.ReadArray<FIoFileIndexEntry>();
            var stringTable = directoryIndex.ReadArray(directoryIndex.ReadFString);

            ref var root = ref directoryEntries[0];

            var files = new Dictionary<string, GameFile>(fileEntries.Length);
            
            ReadIndex(MountPoint, root.FirstChildEntry);
            
            void ReadIndex(string directoryName, uint dir)
            {
                const uint invalidHandle = uint.MaxValue;

                while (dir != invalidHandle)
                {
                    ref var dirEntry = ref directoryEntries[dir];
                    var subDirectoryName = string.Concat(directoryName, stringTable[dirEntry.Name], '/');

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
            
            return Files = files;
        }

        public override byte[] MountPointCheckBytes() => TocResource.DirectoryIndexBuffer ?? new byte[MAX_MOUNTPOINT_TEST_LENGTH];
        protected override byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);

        public override void Dispose()
        {
            Ar.Dispose();
        }
    }
}