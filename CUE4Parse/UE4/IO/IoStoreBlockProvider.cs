using System;
using System.Collections.Generic;

using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.IO;

/// <summary>
/// Provides block-level access to IoStore entries.
/// Unlike Pak files, IoStore uses a global compression block array shared by all entries.
/// This provider maps entry data to the relevant subset of blocks.
/// </summary>
internal sealed class IoStoreBlockProvider : IBlockProvider
{
    private readonly IoStoreReader _reader;
    private readonly FIoStoreEntry _entry;
    private readonly FArchive[] _containerStreams;
    private readonly int _firstBlockIndex;
    private readonly int _lastBlockIndex;
    private readonly long _offsetInFirstBlock;
    private readonly CompressionBlock[] _blocks;

    public int BlockCount => _blocks.Length;
    public long UncompressedSize => _entry.Size;
    public int BlockSize => (int)_reader.TocResource.Header.CompressionBlockSize;
    public CompressionMethod CompressionMethod => _entry.CompressionMethod;
    public bool IsEncrypted => _reader.IsEncrypted;

    /// <summary>
    /// Creates a block provider for an IoStore entry.
    /// </summary>
    /// <param name="entry">The IoStore entry to provide block access for.</param>
    /// <param name="reader">The IoStore reader.</param>
    /// <param name="containerStreams">Container streams to read from. For concurrent access, pass cloned streams.</param>
    public IoStoreBlockProvider(FIoStoreEntry entry, IoStoreReader reader, FArchive[] containerStreams)
    {
        _entry = entry;
        _reader = reader;
        _containerStreams = containerStreams;

        var compressionBlockSize = reader.TocResource.Header.CompressionBlockSize;
        var offset = entry.Offset;
        var length = entry.Size;

        _firstBlockIndex = (int)(offset / compressionBlockSize);
        _lastBlockIndex = (int)(((offset + length).Align((int)compressionBlockSize) - 1) / compressionBlockSize);
        _offsetInFirstBlock = offset % compressionBlockSize;

        _blocks = BuildBlockList();
    }

    private CompressionBlock[] BuildBlockList()
    {
        var tocBlocks = _reader.TocResource.CompressionBlocks;
        var compressionBlockSize = (int)_reader.TocResource.Header.CompressionBlockSize;
        var numBlocks = _lastBlockIndex - _firstBlockIndex + 1;
        var blocks = new CompressionBlock[numBlocks];

        long uncompressedOffset = 0;
        var remainingSize = _entry.Size;

        for (var i = 0; i < numBlocks; i++)
        {
            var globalBlockIndex = _firstBlockIndex + i;
            ref var tocBlock = ref tocBlocks[globalBlockIndex];

            // Calculate uncompressed size for this block relative to the entry
            int uncompressedSize;
            if (i == 0)
            {
                // First block: account for offset within the block
                uncompressedSize = (int)Math.Min(
                    compressionBlockSize - _offsetInFirstBlock,
                    remainingSize);
            }
            else
            {
                uncompressedSize = (int)Math.Min(compressionBlockSize, remainingSize);
            }

            blocks[i] = new CompressionBlock(
                tocBlock.Offset,
                (int)tocBlock.CompressedSize,
                (int)tocBlock.UncompressedSize, // Full block uncompressed size
                uncompressedOffset);

            uncompressedOffset += uncompressedSize;
            remainingSize -= uncompressedSize;
        }

        return blocks;
    }

    public CompressionBlock GetBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= _blocks.Length)
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        return _blocks[blockIndex];
    }

    /// <summary>
    /// Gets the global (TOC-level) block index for a local block index.
    /// </summary>
    public int GetGlobalBlockIndex(int localBlockIndex) => _firstBlockIndex + localBlockIndex;

    /// <summary>
    /// Gets the compression method index for a specific block.
    /// </summary>
    public byte GetCompressionMethodIndex(int blockIndex)
    {
        var globalIndex = GetGlobalBlockIndex(blockIndex);
        return _reader.TocResource.CompressionBlocks[globalIndex].CompressionMethodIndex;
    }

    /// <summary>
    /// Gets the compression method for a specific block.
    /// IoStore blocks may have different compression methods.
    /// </summary>
    public CompressionMethod GetBlockCompressionMethod(int blockIndex)
    {
        var methodIndex = GetCompressionMethodIndex(blockIndex);
        return _reader.TocResource.CompressionMethods[methodIndex];
    }

    /// <summary>
    /// Gets the offset within the first block where entry data starts.
    /// </summary>
    public long OffsetInFirstBlock => _offsetInFirstBlock;

    public int ReadBlockRaw(int blockIndex, Span<byte> buffer)
    {
        var block = GetBlock(blockIndex);
        var readSize = GetBlockReadSize(blockIndex);

        if (buffer.Length < readSize)
            throw new ArgumentException($"Buffer too small. Need {readSize} bytes, got {buffer.Length}.", nameof(buffer));

        // Determine which partition contains this block
        var partitionIndex = (int)((ulong)block.CompressedOffset / _reader.TocResource.Header.PartitionSize);
        var partitionOffset = (long)((ulong)block.CompressedOffset % _reader.TocResource.Header.PartitionSize);

        // Read from the appropriate container stream
        var reader = _containerStreams[partitionIndex];
        var tempBuffer = new byte[readSize];
        reader.ReadAt(partitionOffset, tempBuffer, 0, readSize);
        tempBuffer.AsSpan(0, readSize).CopyTo(buffer);

        return readSize;
    }

    public int GetBlockReadSize(int blockIndex)
    {
        var block = GetBlock(blockIndex);
        // Align read size for encryption
        return IsEncrypted ? block.CompressedSize.Align(Aes.ALIGN) : block.CompressedSize;
    }

    public int FindBlockForOffset(long uncompressedOffset)
    {
        if (uncompressedOffset < 0 || uncompressedOffset >= UncompressedSize)
            return -1;

        // Account for offset in first block
        var adjustedOffset = uncompressedOffset;

        // Calculate which local block contains this offset
        var blockSize = BlockSize;

        // For the first block, we start at _offsetInFirstBlock within the block
        if (adjustedOffset < blockSize - _offsetInFirstBlock)
            return 0;

        // Subsequent blocks
        var offsetAfterFirst = adjustedOffset - (blockSize - _offsetInFirstBlock);
        return 1 + (int)(offsetAfterFirst / blockSize);
    }

    public void Dispose()
    {
        // Don't dispose container streams - they may be shared or owned by the reader
    }
}
