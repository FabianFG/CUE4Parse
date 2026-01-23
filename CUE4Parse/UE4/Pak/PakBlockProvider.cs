using System;

using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Pak;

/// <summary>
/// Provides block-level access to compressed Pak file entries.
/// Enables streaming decompression for large files.
/// </summary>
internal sealed class PakBlockProvider : IBlockProvider
{
    private readonly FPakEntry _entry;
    private readonly FArchive _archive;
    private readonly CompressionBlock[] _blocks;
    private readonly int _encryptionAlignment;

    public int BlockCount => _blocks.Length;
    public long UncompressedSize => _entry.UncompressedSize;
    public int BlockSize => (int)_entry.CompressionBlockSize;
    public CompressionMethod CompressionMethod => _entry.CompressionMethod;
    public bool IsEncrypted => _entry.IsEncrypted;

    /// <summary>
    /// Creates a block provider for a Pak entry.
    /// </summary>
    /// <param name="entry">The Pak entry to provide block access for.</param>
    /// <param name="archive">The archive to read from. If concurrent access is needed, pass a cloned archive.</param>
    public PakBlockProvider(FPakEntry entry, FArchive archive)
    {
        _entry = entry;
        _archive = archive;
        _encryptionAlignment = entry.IsEncrypted ? Aes.ALIGN : 1;
        _blocks = BuildBlockList(entry);
    }

    private static CompressionBlock[] BuildBlockList(FPakEntry entry)
    {
        var sourceBlocks = entry.CompressionBlocks;
        if (sourceBlocks.Length == 0)
            return [];

        var blocks = new CompressionBlock[sourceBlocks.Length];
        var blockSize = (int)entry.CompressionBlockSize;
        long uncompressedOffset = 0;

        for (var i = 0; i < sourceBlocks.Length; i++)
        {
            var sourceBlock = sourceBlocks[i];
            var compressedSize = (int)sourceBlock.Size;

            // Last block may be smaller than block size
            var uncompressedSize = (int)Math.Min(
                blockSize,
                entry.UncompressedSize - uncompressedOffset);

            blocks[i] = new CompressionBlock(
                sourceBlock.CompressedStart,
                compressedSize,
                uncompressedSize,
                uncompressedOffset);

            uncompressedOffset += uncompressedSize;
        }

        return blocks;
    }

    public CompressionBlock GetBlock(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= _blocks.Length)
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        return _blocks[blockIndex];
    }

    public int ReadBlockRaw(int blockIndex, Span<byte> buffer)
    {
        var block = GetBlock(blockIndex);
        var readSize = GetBlockReadSize(blockIndex);

        if (buffer.Length < readSize)
            throw new ArgumentException($"Buffer too small. Need {readSize} bytes, got {buffer.Length}.", nameof(buffer));

        // Read directly into the provided buffer
        _archive.Position = block.CompressedOffset;
        var tempBuffer = _archive.ReadBytes(readSize);
        tempBuffer.AsSpan(0, readSize).CopyTo(buffer);

        return readSize;
    }

    public int GetBlockReadSize(int blockIndex)
    {
        var block = GetBlock(blockIndex);
        // Align read size for encryption
        return block.CompressedSize.Align(_encryptionAlignment);
    }

    public int FindBlockForOffset(long uncompressedOffset)
    {
        if (uncompressedOffset < 0 || uncompressedOffset >= UncompressedSize)
            return -1;

        // Fast path: calculate based on block size (works for all but potentially the last block)
        var estimatedIndex = (int)(uncompressedOffset / BlockSize);
        if (estimatedIndex < _blocks.Length)
        {
            var block = _blocks[estimatedIndex];
            if (uncompressedOffset >= block.UncompressedOffset &&
                uncompressedOffset < block.UncompressedOffset + block.UncompressedSize)
            {
                return estimatedIndex;
            }
        }

        // Fallback: linear search (should rarely be needed)
        for (var i = 0; i < _blocks.Length; i++)
        {
            var block = _blocks[i];
            if (uncompressedOffset >= block.UncompressedOffset &&
                uncompressedOffset < block.UncompressedOffset + block.UncompressedSize)
            {
                return i;
            }
        }

        return -1;
    }

    public void Dispose()
    {
        // Don't dispose the archive - it may be shared or owned by the reader
        // The caller is responsible for archive lifecycle
    }
}
