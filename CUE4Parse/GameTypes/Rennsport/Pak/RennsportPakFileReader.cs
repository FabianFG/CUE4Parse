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
    private byte[] RennsportCompressedExtract(FArchive reader, FPakEntry pakEntry)
    {
        var uncompressed = new byte[(int) pakEntry.UncompressedSize];
        var size = (int) pakEntry.CompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
        reader.Position = pakEntry.CompressionBlocks.First().CompressedStart;
        var full = DecryptIfEncrypted(reader.ReadBytes(size),0, size, pakEntry.IsEncrypted, true);

        var fullOffset = 0;
        var uncompressedOff = 0;
        foreach (var block in pakEntry.CompressionBlocks)
        {
            var blockSize = (int) block.Size;
            var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
            var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
            Decompress(full, fullOffset, srcSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
            uncompressedOff += (int) pakEntry.CompressionBlockSize;
            fullOffset += srcSize;
        }

        return uncompressed;
    }

    private byte[] RennsportExtract(FArchive reader, FPakEntry pakEntry)
    {
        reader.Position = pakEntry.Offset + pakEntry.StructSize; // Doesn't seem to be the case with older pak versions
        var size = (int) pakEntry.UncompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
        var data = DecryptIfEncrypted(reader.ReadBytes(size),0, size, pakEntry.IsEncrypted, true);
        return size != pakEntry.UncompressedSize ? data.SubByteArray((int) pakEntry.UncompressedSize) : data;
    }
}
