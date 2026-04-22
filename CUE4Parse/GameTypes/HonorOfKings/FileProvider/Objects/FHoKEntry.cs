using System;
using System.Linq;
using CUE4Parse.Compression;
using CUE4Parse.GameTypes.HonorOfKings.Lua;
using CUE4Parse.GameTypes.HonorOfKings.Vfs;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.GameTypes.HonorOfKings.FileProvider.Objects;

public sealed class FHoKEntry : FPakEntry
{
    private readonly HoKdbContainerStream Container;
    public readonly ulong Hash;

    public FHoKEntry(IVfsReader vfs, HoKdbContainerStream container, string path, ulong hash, byte flags) : base(vfs, path)
    {
        CompressionMethod = flags switch
        {
            0 => CompressionMethod.None,
            1 => CompressionMethod.LZ4,
            3 => CompressionMethod.Oodle,
            _ => throw new ParserException("Unknown entry compression flag"),
        };
        Hash = hash;
        Container = container;
        Size = container.CompressedChunks[hash].Sum(x => (long)x.UncompressedSize);
    }

    public override bool IsEncrypted => false;

    public override CompressionMethod CompressionMethod { get; }

    public override FArchive CreateReader(FByteBulkDataHeader? header = null)
    {
        var data = Read(header);
        return new FByteArchive(Path, data, Vfs.Versions);
    }

    public override byte[] Read(FByteBulkDataHeader? header = null)
    {
        var Ar = Container;
        long offset = 0;
        var requestedSize = (int) Size;
        if (header is { } bulk)
        {
            if (bulk.BulkDataFlags.HasFlag(EBulkDataFlags.BULKDATA_WorkspaceDomainPayload) && Extension.Equals("ubulk", StringComparison.OrdinalIgnoreCase))
            {
                var path = System.IO.Path.ChangeExtension(Path, ".g.ubulk");
                if (Vfs is HoKdbFileReader reader && reader.Provider.TryGetGameFile(path, out var gbulk))
                {
                    return gbulk.Read(header);
                }
            }

            offset = bulk.OffsetInFile;
            requestedSize = (int) bulk.SizeOnDisk;
        }

        var blocks = Container.CompressedChunks[Hash];

        if (CompressionMethod != CompressionMethod.None)
        {
            const int compressionBlockSize = 65536;
            var firstBlockIndex = offset / compressionBlockSize;
            var lastBlockIndex = (offset + requestedSize - 1) / compressionBlockSize;

            var numBlocks = lastBlockIndex - firstBlockIndex + 1;
            var bufferSize = numBlocks * compressionBlockSize;
            if (lastBlockIndex == (int)((Size - 1) / compressionBlockSize))
            {
                var lastBlockInFileSize = (int)(Size % compressionBlockSize);
                if (lastBlockInFileSize > 0)
                    bufferSize -= compressionBlockSize - lastBlockInFileSize;
            }

            var uncompressed = new byte[bufferSize];
            var uncompressedOff = 0;

            var compressedBuffer = Array.Empty<byte>();
            for (var blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
            {
                var block = blocks[blockIndex];
                var blockSize = block.CompressedSize;
                if (blockSize > compressedBuffer.Length)
                {
                    compressedBuffer = new byte[blockSize];
                }

                var uncompressedSize = block.UncompressedSize;
                Ar.ReadAt(block.Offset, compressedBuffer, 0, blockSize);
                Decompress(compressedBuffer, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, CompressionMethod, Ar);
                uncompressedOff += uncompressedSize;
            }

            if (Extension is "lua")
                _ = new NGRLuaReader(Path, uncompressed, out uncompressed);

            var offsetInFirstBlock = offset - firstBlockIndex * compressionBlockSize;
            if (offsetInFirstBlock == 0 && requestedSize == bufferSize)
                return uncompressed;

            var result = new byte[requestedSize];
            Array.Copy(uncompressed, offsetInFirstBlock, result, 0, requestedSize);
            return result;
        }

        var data = new byte[Size];
        var uncompressedOffset = 0;
        foreach (var part in blocks)
        {
            var compSize = part.CompressedSize;
            var uncompSize = part.UncompressedSize;
            var bytes = Ar.ReadBytesAt(part.Offset, compSize);
            Decompress(bytes, 0, compSize, data, uncompressedOffset, uncompSize, CompressionMethod, Ar);
            uncompressedOffset += uncompSize;
        }

        if (Extension is "lua")
            _ = new NGRLuaReader(Path, data, out data);

        if (offset == 0 && requestedSize == data.Length)
            return data;

        var chunk = new byte[requestedSize];
        Array.Copy(data, offset, chunk, 0, requestedSize);
        return chunk;
    }
}
