using CUE4Parse.Compression;
using CUE4Parse.GameTypes.MarchOfGiants.Vfs;
using CUE4Parse.GameTypes.MarchOfGiants.Vfs.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.MarchOfGiants.FileProvider.Objects;

internal sealed class FMogEntry : FPakEntry
{
    private readonly MogFileReader _reader;
    public readonly FMogChunk[] Chunks;
    public override bool IsEncrypted => true;
    public override CompressionMethod CompressionMethod { get; }

    public FMogEntry(MogFileReader reader, FByteArchive toc, Dictionary<FGuid, FMogBlock> blocks) : base(reader, "")
    {
        _reader = reader;
        Path = toc.ReadFString().NormalizePath();
        Size = toc.Read<long>();
        _ = toc.Read<FGuid>(); // File hash
        var chunkCount = toc.Read<int>();
        if (Size < 0 || chunkCount < 0)
            throw new ParserException(toc, "Invalid MOG file size or chunk count");

        CompressionMethod = CompressionMethod.None;

        long totalSize = 0;
        Chunks = new FMogChunk[chunkCount];
        for (var i = 0; i < chunkCount; i++)
        {
            var hash = toc.Read<FGuid>();
            var offset = toc.Read<int>();
            var chunkSize = toc.Read<int>();
            if (offset < -1 || chunkSize < 0)
                throw new ParserException(toc, "Invalid MOG chunk range");

            offset = Math.Max(offset, 0);
            var block = hash.IsValid() ? blocks.GetValueOrDefault(hash) ?? throw new ParserException(toc, $"Missing MOG block {hash}") : null;
            if (block is not null && (long) offset + chunkSize > block.Size)
                throw new ParserException(toc, "MOG chunk exceeds its block");
            if (block is { IsCompressed: true })
                CompressionMethod = CompressionMethod.Zstd;

            Chunks[i] = new FMogChunk(block, offset, chunkSize);
            totalSize += chunkSize;
        }

        if (totalSize != Size)
            throw new ParserException(toc, $"MOG chunk sizes don't match file size for {Path}");
    }

    public override byte[] Read(FByteBulkDataHeader? header = null)
    {
        var offset = header?.OffsetInFile ?? 0;
        var size = header?.SizeOnDisk ?? Size;
        if (offset < 0 || size < 0 || offset > Size - size)
            throw new ArgumentOutOfRangeException(nameof(header), "Requested range exceeds the MOG file");

        var result = new byte[checked((int) size)];
        var written = 0;
        foreach (var chunk in Chunks)
        {
            if (written == result.Length)
                break;
            if (offset >= chunk.Size)
            {
                offset -= chunk.Size;
                continue;
            }

            var count = (int) Math.Min(chunk.Size - offset, result.Length - written);
            if (chunk.Block is { } block)
            {
                var blockOffset = chunk.Offset + (int) offset;
                if (block.IsCompressed)
                {
                    _reader.GetBlock(block).AsSpan(blockOffset, count).CopyTo(result.AsSpan(written));
                }
                else
                {
                    _reader.ReadRawRange(block, blockOffset, result, written, count);
                }
            }

            written += count;
            offset = 0;
        }

        return result;
    }
}
