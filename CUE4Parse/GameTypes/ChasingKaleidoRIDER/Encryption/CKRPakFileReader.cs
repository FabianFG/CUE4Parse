using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.ChasingKaleidoRIDER.Encryption;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public byte[] CKRExtract(FArchive reader, FPakEntry pakEntry, FByteBulkDataHeader? header = null)
    {
        var alignment = pakEntry.IsEncrypted ? Aes.ALIGN : 1;
        var encryptionBaseOffset = pakEntry.Offset + pakEntry.StructSize;
        long offset = 0;
        var requestedSize = (int) pakEntry.UncompressedSize;
        if (header is { } bulk)
        {
            offset = bulk.OffsetInFile;
            requestedSize = (int) bulk.SizeOnDisk;
        }

        if (pakEntry.IsCompressed)
        {
            var compressionBlockSize = (int) pakEntry.CompressionBlockSize;
            var firstBlockIndex = offset / compressionBlockSize;
            var lastBlockIndex = (offset + requestedSize - 1) / compressionBlockSize;

            // blocks are full size, except potentially the last one
            var numBlocks = lastBlockIndex - firstBlockIndex + 1;
            var bufferSize = numBlocks * compressionBlockSize;
            if (lastBlockIndex == (int) ((pakEntry.UncompressedSize - 1) / compressionBlockSize))
            {
                var lastBlockInFileSize = (int) (pakEntry.UncompressedSize % compressionBlockSize);
                if (lastBlockInFileSize > 0)
                    bufferSize -= compressionBlockSize - lastBlockInFileSize;
            }

            var uncompressed = new byte[bufferSize];
            var uncompressedOff = 0;

            var compressedBuffer = Array.Empty<byte>();
            // decompress the required blocks
            for (var blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
            {
                var block = pakEntry.CompressionBlocks[blockIndex];
                var blockSize = (int) block.Size;
                var srcSize = blockSize.Align(alignment);
                if (srcSize > compressedBuffer.Length)
                {
                    compressedBuffer = new byte[srcSize];
                }
                reader.ReadAt(block.CompressedStart, compressedBuffer, 0, srcSize);
                var index = (block.CompressedStart - encryptionBaseOffset) >> 6;
                var blockOffset = (int) ((block.CompressedStart - encryptionBaseOffset) % 64);
                var compressed = pakEntry.IsEncrypted
                    ? CKREncryption.CKRDecrypt(compressedBuffer, 0, srcSize, index, encryptionBaseOffset, this, blockOffset)
                    : compressedBuffer;
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

        var readSize = (int) pakEntry.UncompressedSize.Align(alignment);
        var encryptedBuffer = new byte[readSize];
        reader.ReadAt(encryptionBaseOffset, encryptedBuffer, 0, readSize);
        var data = pakEntry.IsEncrypted
            ? CKREncryption.CKRDecrypt(encryptedBuffer, 0, readSize, 0, encryptionBaseOffset, this)
            : encryptedBuffer;


        return requestedSize != readSize ? data[(int)offset..(int)(offset + requestedSize)] : data;
    }
}
