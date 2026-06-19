using CUE4Parse.GameTypes.ProSpi.Encryption.Aes;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public byte[] ProSpiExtract(FArchive reader, FPakEntry pakEntry, int alignment, FByteBulkDataHeader? header, long offset, int requestedSize)
    {
        if (pakEntry.IsCompressed)
        {
            var compressionBlockSize = (int) pakEntry.CompressionBlockSize;
            var firstBlockIndex = offset / compressionBlockSize;
            var lastBlockIndex = (offset + requestedSize - 1) / compressionBlockSize;

            var numBlocks = lastBlockIndex - firstBlockIndex + 1;
            var bufferSize = (int) (numBlocks * compressionBlockSize);

            if (lastBlockIndex == (pakEntry.UncompressedSize - 1) / compressionBlockSize)
            {
                var lastBlockInFileSize = (int) (pakEntry.UncompressedSize % compressionBlockSize);
                if (lastBlockInFileSize > 0)
                    bufferSize -= compressionBlockSize - lastBlockInFileSize;
            }

            var uncompressed = new byte[bufferSize];
            var uncompressedOff = 0;
            for (var blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
            {
                var block = pakEntry.CompressionBlocks[blockIndex];
                var blockSize = (int) block.Size;

                var srcSize = pakEntry.IsEncrypted && header is null ? (blockSize + ProSpiEncryption.EncryptionDataTrailerSize).Align(alignment) : blockSize.Align(alignment);
                var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                var uncompressedSize = (int) Math.Min(compressionBlockSize, pakEntry.UncompressedSize - blockIndex * compressionBlockSize);

                Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);

                uncompressedOff += uncompressedSize;
            }

            var offsetInFirstBlock = offset - firstBlockIndex * compressionBlockSize;

            if (offsetInFirstBlock == 0 && requestedSize == bufferSize)
                return uncompressed;

            var result = new byte[requestedSize];
            Array.Copy(uncompressed, offsetInFirstBlock, result, 0, requestedSize);
            return result;
        }

        var size = pakEntry.IsEncrypted && header is null ? (pakEntry.UncompressedSize + ProSpiEncryption.EncryptionDataTrailerSize).Align(alignment) : pakEntry.UncompressedSize.Align(alignment);
        var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize, (int) size, reader, pakEntry.IsEncrypted);

        if (size != pakEntry.UncompressedSize)
            data = data.SubByteArray((int) pakEntry.UncompressedSize);
        if (offset == 0 && requestedSize == data.Length)
            return data;

        var chunk = new byte[requestedSize];
        Array.Copy(data, offset, chunk, 0, requestedSize);
        return chunk;
    }
}
