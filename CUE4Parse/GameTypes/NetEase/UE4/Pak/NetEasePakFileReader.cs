// ReSharper disable CheckNamespace
using System;
using System.Linq;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    /// <summary>
    /// Function for extracting an entry from the pak in NetEase games
    /// Their games only encrypt the first 4kb of the block, the rest is unencrypted,
    /// thus requiring this custom implementation.
    /// </summary>
    /// <param name="reader">The pak reader</param>
    /// <param name="pakEntry">The entry to be extracted</param>
    /// <returns>The merged and decompressed/decrypted entry data</returns>
    private byte[] NetEaseExtract(FArchive reader, FPakEntry pakEntry)
    {
        var uncompressed = new byte[(int) pakEntry.UncompressedSize];
        var uncompressedOff = 0;
        var limit = 0x1000;

        Span<byte> compressedBuffer = stackalloc byte[pakEntry.CompressionBlocks.Max(block => (int) block.Size.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1))];

        foreach (var block in pakEntry.CompressionBlocks)
        {
            reader.Position = block.CompressedStart;
            var blockSize = (int) block.Size;
            var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);

            // Read the encrypted block
            var compressed = compressedBuffer[..srcSize];
            ReadAndDecrypt(blockSize < limit && limit > 0 ? srcSize : limit, reader, pakEntry.IsEncrypted).CopyTo(compressed);

            // Remaining size is unencrypted
            if (blockSize > limit)
            {
                var diff = blockSize - limit;
                reader.ReadBytes(diff).CopyTo(compressed[limit..]);
                limit = srcSize;
            }

            // Calculate the uncompressed size,
            // its either just the compression block size or if it's the last block
            var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);

            Decompress(compressed.ToArray(), 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
            uncompressedOff += (int) pakEntry.CompressionBlockSize;
            limit -= srcSize;
        }

        return uncompressed;
    }
}