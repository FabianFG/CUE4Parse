using CUE4Parse.GameTypes.ProSpi.Encryption.Aes;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public byte[] ProSpiExtract(FArchive reader, FPakEntry pakEntry, int alignment, FByteBulkDataHeader? header)
    {
        if (pakEntry.IsCompressed)
        {
            var uncompressed = new byte[(int) pakEntry.UncompressedSize];
            var uncompressedOff = 0;
            foreach (var block in pakEntry.CompressionBlocks)
            {
                var blockSize = (int) block.Size;
                var srcSize = pakEntry.IsEncrypted ? (blockSize + ProSpiEncryption.EncryptionDataTrailerSize).Align(alignment) : blockSize.Align(alignment);
                var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
                Decompress(compressed, 0, srcSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += (int) pakEntry.CompressionBlockSize;
            }

            return uncompressed;
        }

        var size = pakEntry.IsEncrypted && header is null ? (pakEntry.UncompressedSize + ProSpiEncryption.EncryptionDataTrailerSize).Align(alignment) : pakEntry.UncompressedSize.Align(alignment);
        var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize, (int) size, reader, pakEntry.IsEncrypted);
        return size != pakEntry.UncompressedSize ? data.SubByteArray((int) pakEntry.UncompressedSize) : data;
    }
}
