namespace CUE4Parse.UE4.VirtualFileSystem;

/// <summary>
/// Represents a single compression block within an archive entry.
/// Used for streaming decompression of large files.
/// </summary>
public readonly struct CompressionBlock
{
    /// <summary>
    /// Absolute offset of the compressed data within the archive.
    /// </summary>
    public long CompressedOffset { get; init; }

    /// <summary>
    /// Size of the compressed data in bytes.
    /// </summary>
    public int CompressedSize { get; init; }

    /// <summary>
    /// Size of the uncompressed data in bytes.
    /// </summary>
    public int UncompressedSize { get; init; }

    /// <summary>
    /// Offset of this block's uncompressed data within the logical file.
    /// </summary>
    public long UncompressedOffset { get; init; }

    public CompressionBlock(long compressedOffset, int compressedSize, int uncompressedSize, long uncompressedOffset)
    {
        CompressedOffset = compressedOffset;
        CompressedSize = compressedSize;
        UncompressedSize = uncompressedSize;
        UncompressedOffset = uncompressedOffset;
    }
}
