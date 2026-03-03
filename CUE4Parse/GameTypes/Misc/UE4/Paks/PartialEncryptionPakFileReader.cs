// ReSharper disable CheckNamespace

using System;
using System.Linq;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    /// <summary>
    /// Function for extracting an entry from the pak file that uses partial block encryption
    /// Some games encrypt only the first part of the block,
    /// thus requiring this custom implementation.
    /// </summary>
    /// <param name="reader">The pak reader</param>
    /// <param name="pakEntry">The entry to be extracted</param>
    /// <param name="header">Optional bulk data header</param>
    /// <returns>The merged and decompressed/decrypted entry data</returns>
    private byte[] PartialEncryptCompressedExtract(FArchive reader, FPakEntry pakEntry, FByteBulkDataHeader? header = null)
    {
        var limit = Game switch
        {
            EGame.GAME_MarvelRivals => CalculateEncryptedBytesCountForMarvelRivals(pakEntry),
            EGame.GAME_OperationApocalypse or EGame.GAME_MindsEye => 0x1000,
            EGame.GAME_WutheringWaves => CalculateEncryptedBytesCountForWutheringWaves(pakEntry),
            _ => throw new ArgumentOutOfRangeException(nameof(reader.Game), "Unsupported game for partial encrypted pak entry extraction")
        };

        var uncompressedOff = 0;
        var compressedBuffer = new byte[pakEntry.CompressionBlocks.Max(block => (int) block.Size.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1))];
        var compSpan = compressedBuffer.AsSpan();

        if (header is { } bulk)
        {
            var uncompressedBuffer = new byte[pakEntry.CompressionBlockSize];
            var offset = bulk.OffsetInFile;
            var requestedSize = (int) bulk.SizeOnDisk;
            var result = new byte[requestedSize];
            var compressionBlockSize = (int) pakEntry.CompressionBlockSize;
            var firstBlockIndex = offset / compressionBlockSize;
            var lastBlockIndex = (offset + requestedSize - 1) / compressionBlockSize;
            var lastBlock = pakEntry.CompressionBlocks.Length - 1;
            
            for (var i = 0; i < firstBlockIndex; i++)
            {
                if (limit > 0)
                {
                    var blockSize = (int) pakEntry.CompressionBlocks[i].Size;
                    limit -= blockSize.Align(pakEntry.IsEncrypted && limit >= blockSize ? Aes.ALIGN : 1);
                }
                else
                    break;
            }

            if (limit <= 0) limit = 0;

            offset -= firstBlockIndex * compressionBlockSize;
            for (var i = firstBlockIndex; i <= lastBlockIndex; i++)
            {
                var block = pakEntry.CompressionBlocks[i];
                var blockSize = (int) block.Size;
                var srcSize = blockSize.Align(pakEntry.IsEncrypted && limit >= blockSize ? Aes.ALIGN : 1);

                var bytesToRead = blockSize < limit && limit > 0 ? srcSize : limit;
                var compressed = compSpan[..srcSize];
                ReadAndDecryptAt(block.CompressedStart, bytesToRead, reader, pakEntry.IsEncrypted).CopyTo(compSpan);

                if (blockSize > limit)
                {
                    var diff = blockSize - limit;
                    reader.ReadBytesAt(block.CompressedStart + bytesToRead, diff).CopyTo(compSpan[limit..]);
                    limit = srcSize;
                }

                var uncompressedSize = i == lastBlock ? (int) (pakEntry.UncompressedSize - lastBlock * compressionBlockSize) : compressionBlockSize;
                Decompress(compressedBuffer, 0, blockSize, uncompressedBuffer, 0, uncompressedSize, pakEntry.CompressionMethod);

                var copySize = Math.Min((int)(uncompressedSize - offset), requestedSize);

                Buffer.BlockCopy(uncompressedBuffer, (int) offset, result, uncompressedOff, copySize);
                uncompressedOff += copySize;
                offset = 0;
                requestedSize -= copySize;
                limit -= srcSize;
            }
            return result;
        }

        var uncompressed = new byte[(int) pakEntry.UncompressedSize];
        foreach (var block in pakEntry.CompressionBlocks)
        {
            var blockSize = (int) block.Size;
            var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);

            // Read the encrypted block
            var compressed = compSpan[..srcSize];
            var bytesToRead = blockSize < limit && limit > 0 ? srcSize : limit;
            ReadAndDecryptAt(block.CompressedStart, bytesToRead, reader, pakEntry.IsEncrypted).CopyTo(compSpan);

            // Remaining size is unencrypted
            if (blockSize > limit)
            {
                var diff = blockSize - limit;
                reader.ReadBytesAt(block.CompressedStart + bytesToRead, diff).CopyTo(compSpan[limit..]);
                limit = srcSize;
            }

            // Calculate the uncompressed size,
            // its either just the compression block size or if it's the last block
            var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);

            Decompress(compressedBuffer, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
            uncompressedOff += (int) pakEntry.CompressionBlockSize;
            limit -= srcSize;
        }

        return uncompressed;
    }

    private byte[] PartialEncryptExtract(FArchive reader, FPakEntry pakEntry, FByteBulkDataHeader? header = null)
    {
        var limit = Game switch
        {
            EGame.GAME_MarvelRivals => CalculateEncryptedBytesCountForMarvelRivals(pakEntry),
            EGame.GAME_OperationApocalypse or EGame.GAME_MindsEye => 0x1000,
            EGame.GAME_WutheringWaves => CalculateEncryptedBytesCountForWutheringWaves(pakEntry),
            _ => throw new ArgumentOutOfRangeException(nameof(reader.Game), "Unsupported game for partial encrypted pak entry extraction")
        };

        var alignment = pakEntry.IsEncrypted ? Aes.ALIGN : 1;
        int size = 0;

        if (header is { } bulk)
        {
            var offset = bulk.OffsetInFile;
            if (offset > limit)
            {
                return reader.ReadBytesAt(pakEntry.Offset + pakEntry.StructSize + offset, (int)bulk.SizeOnDisk);
            }
            else
            {
                var readOffset = offset & ~((long) alignment - 1);
                var dataOffset = offset - readOffset;
                var readSize = (dataOffset + (int) bulk.SizeOnDisk).Align(alignment);
                limit -= (int)readOffset;
                var bytesToReadpart = readSize <= limit ? readSize : limit;
                var encryptedpart = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize + readOffset, (int)bytesToReadpart, reader, pakEntry.IsEncrypted);
                encryptedpart = dataOffset == 0 ? encryptedpart : encryptedpart[(int)dataOffset..];
                bytesToReadpart = (int) (bulk.SizeOnDisk - encryptedpart.Length);

                if (bytesToReadpart > 0)
                {
                    var normalpart = reader.ReadBytesAt(pakEntry.Offset + pakEntry.StructSize + readOffset + limit, (int)bytesToReadpart);
                    return [.. encryptedpart, .. normalpart];
                }

                return encryptedpart[..(int) bulk.SizeOnDisk];
            }
        }

        size = (int) pakEntry.UncompressedSize.Align(alignment);
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

    private int CalculateEncryptedBytesCountForWutheringWaves(FPakEntry pakEntry)
    {
        return pakEntry.CustomData switch
        {
            0 => int.MaxValue,
            1 => 0x200000,
            2 => 0x800,
            _ => throw new NotImplementedException($"Unknown value of WutheringWaves PakEntry byte {pakEntry.CustomData} for partially encrypted file")
        };
    }
}
