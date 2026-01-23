using System;

using CUE4Parse.Compression;

namespace CUE4Parse.UE4.VirtualFileSystem;

/// <summary>
/// Provides block-level access to compressed archive entries.
/// Enables streaming decompression and lazy loading of large files.
/// </summary>
public interface IBlockProvider : IDisposable
{
    /// <summary>
    /// Total number of compression blocks in this entry.
    /// </summary>
    int BlockCount { get; }

    /// <summary>
    /// Total uncompressed size of all blocks combined.
    /// </summary>
    long UncompressedSize { get; }

    /// <summary>
    /// Typical size of each compression block (except possibly the last).
    /// </summary>
    int BlockSize { get; }

    /// <summary>
    /// Compression method used for this entry's blocks.
    /// </summary>
    CompressionMethod CompressionMethod { get; }

    /// <summary>
    /// Whether the blocks are encrypted and require decryption.
    /// </summary>
    bool IsEncrypted { get; }

    /// <summary>
    /// Gets metadata for a specific compression block.
    /// </summary>
    /// <param name="blockIndex">Zero-based block index.</param>
    /// <returns>Block metadata including offsets and sizes.</returns>
    CompressionBlock GetBlock(int blockIndex);

    /// <summary>
    /// Reads raw (compressed and possibly encrypted) block data.
    /// </summary>
    /// <param name="blockIndex">Zero-based block index.</param>
    /// <param name="buffer">Buffer to receive the raw data. Must be at least CompressedSize bytes.</param>
    /// <returns>Number of bytes read.</returns>
    int ReadBlockRaw(int blockIndex, Span<byte> buffer);

    /// <summary>
    /// Gets the aligned read size for a block (accounts for encryption alignment).
    /// </summary>
    /// <param name="blockIndex">Zero-based block index.</param>
    /// <returns>Size in bytes needed to read the block (may be larger than CompressedSize due to alignment).</returns>
    int GetBlockReadSize(int blockIndex);

    /// <summary>
    /// Finds the block index containing the given uncompressed offset.
    /// </summary>
    /// <param name="uncompressedOffset">Offset within the uncompressed data.</param>
    /// <returns>Block index, or -1 if offset is out of range.</returns>
    int FindBlockForOffset(long uncompressedOffset);
}
