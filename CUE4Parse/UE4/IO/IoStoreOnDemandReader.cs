using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO
{
    public class IoStoreOnDemandReader : IoStoreReader
    {
        public readonly FOnDemandTocEntry[] Entries;

        private readonly IoStoreOnDemandDownloader _downloader;

        public IoStoreOnDemandReader(FArchive tocStream, FOnDemandTocEntry[] entries, IoStoreOnDemandDownloader downloader)
            : base(tocStream, it => new FByteArchive(it, Array.Empty<byte>(), tocStream.Versions))
        {
            Entries = entries;
            _downloader = downloader;
        }

        public override byte[] Extract(VfsEntry entry)
        {
            if (!(entry is FIoStoreEntry ioEntry) || entry.Vfs != this) throw new ArgumentException($"Wrong io store reader, required {entry.Vfs.Path}, this is {Path}");
            return Read(Entries[ioEntry.TocEntryIndex]);
        }

        public override byte[] Read(FIoChunkId chunkId) => Read(Entries.FirstOrDefault(entry => entry.ChunkId == chunkId));

        private byte[] Read(FOnDemandTocEntry? onDemandEntry)
        {
            if (onDemandEntry == null) throw new ParserException("Can't read unknown on-demand entry");
            if (TryResolve(onDemandEntry.ChunkId, out var offsetLength))
            {
                return Read(onDemandEntry.Hash.ToString().ToLower(), (long) offsetLength.Offset, (long) offsetLength.Length);
            }
            throw new KeyNotFoundException($"Couldn't find chunk {onDemandEntry.ChunkId} in IoStoreOnDemand {Name}");
        }

        private byte[] Read(string hash, long offset, long length)
        {
            var reader = _downloader.Download($"chunks/{hash[..2]}/{hash}.iochunk").GetAwaiter().GetResult();

            var compressionBlockSize = TocResource.Header.CompressionBlockSize;
            var dst = new byte[length];
            var firstBlockIndex = (int) (offset / compressionBlockSize);
            var lastBlockIndex = (int) (((offset + dst.Length).Align((int) compressionBlockSize) - 1) / compressionBlockSize);
            var offsetInBlock = offset % compressionBlockSize;
            var remainingSize = length;
            var dstOffset = 0;

            var compressedBuffer = Array.Empty<byte>();
            var uncompressedBuffer = Array.Empty<byte>();

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
                    Compression.Compression.Decompress(compressedBuffer, 0, (int) rawSize, uncompressedBuffer, 0, (int) uncompressedSize, compressionMethod);
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

        public override void Dispose()
        {
            base.Dispose();
            _downloader.Dispose();
        }
    }
}
