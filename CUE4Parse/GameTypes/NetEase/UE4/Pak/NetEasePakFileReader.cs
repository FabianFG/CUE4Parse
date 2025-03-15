// ReSharper disable CheckNamespace

using System;
using System.Linq;
using System.Text;
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
    private byte[] NetEaseCompressedExtract(FArchive reader, FPakEntry pakEntry)
    {
        var uncompressed = new byte[(int) pakEntry.UncompressedSize];
        var uncompressedOff = 0;
        var limit = reader.Game == UE4.Versions.EGame.GAME_MarvelRivals ? CalculateEncryptedBytesCountForMarvelRivals(pakEntry) : 0x1000;

        Span<byte> compressedBuffer = stackalloc byte[pakEntry.CompressionBlocks.Max(block => (int) block.Size.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1))];

        foreach (var block in pakEntry.CompressionBlocks)
        {
            var blockSize = (int) block.Size;
            var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);

            // Read the encrypted block
            var compressed = compressedBuffer[..srcSize];
            var bytesToRead = blockSize < limit && limit > 0 ? srcSize : limit;
            ReadAndDecryptAt(block.CompressedStart, bytesToRead, reader, pakEntry.IsEncrypted).CopyTo(compressed);

            // Remaining size is unencrypted
            if (blockSize > limit)
            {
                var diff = blockSize - limit;
                reader.ReadBytesAt(block.CompressedStart + bytesToRead, diff).CopyTo(compressed[limit..]);
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

    private byte[] NetEaseExtract(FArchive reader, FPakEntry pakEntry)
    {
        var limit = reader.Game == UE4.Versions.EGame.GAME_MarvelRivals ? CalculateEncryptedBytesCountForMarvelRivals(pakEntry) : 0x1000;
        var size = (int) pakEntry.UncompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
        var bytesToRead = size <= limit ? size : limit;
        var encrypted = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize, bytesToRead, reader, pakEntry.IsEncrypted);

        if (size > limit)
        {
            var decrypted = reader.ReadBytesAt(pakEntry.Offset + pakEntry.StructSize + bytesToRead, (int) pakEntry.UncompressedSize - limit);
            return encrypted.Concat(decrypted).ToArray();
        }

        return encrypted[..(int) pakEntry.UncompressedSize];
    }

    private int CalculateEncryptedBytesCountForMarvelRivals(FPakEntry pakEntry)
    {
        using var hasher = Blake3.Hasher.New();

        var initialSeedBytes = BitConverter.GetBytes(0x44332211);
        hasher.Update(initialSeedBytes);

        var assetPathBytes = Encoding.UTF8.GetBytes(pakEntry.Path.ToLower());
        hasher.Update(assetPathBytes);

        var finalHash = hasher.Finalize().AsSpan();

        var firstU64 = BitConverter.ToUInt64(finalHash);

        var final = (63 * (firstU64 % 0x3D) + 319) & 0xFFFFFFFFFFFFFFC0u;

        return (int) final;
    }
}