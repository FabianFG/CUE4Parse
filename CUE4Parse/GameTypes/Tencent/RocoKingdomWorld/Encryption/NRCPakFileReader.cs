using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.ABI.Encryption.SM4;
using CUE4Parse.GameTypes.LordOfMysteries.UE4.Lua;
using CUE4Parse.GameTypes.Netmarble.NiNoKuni.UE4.Encryption;
using CUE4Parse.GameTypes.NFS.Mobile.Lua;
using CUE4Parse.GameTypes.NTE.Encryption;
using CUE4Parse.GameTypes.PUBG.UE4.Lua;
using CUE4Parse.GameTypes.Snowbreak.Encryption.Lua;
using CUE4Parse.GameTypes.Strinova.Lua;
using CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;
using CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Lua;
using CUE4Parse.GameTypes.Tencent.ValorantSource.Lua;
using CUE4Parse.GameTypes.UDWN.Lua;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.Utils;
using Serilog.Parsing;

namespace CUE4Parse.UE4.Pak;

public partial class PakFileReader
{
    public byte[] NRCExtract(FArchive reader, FPakEntry pakEntry, FByteBulkDataHeader? header = null)
    {
        var alignment = pakEntry.IsEncrypted ? Aes.ALIGN : 1;

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

                // Read the compressed block
                reader.ReadAt(block.CompressedStart, compressedBuffer, 0, srcSize);

                var compressed = pakEntry.IsEncrypted
                    ? FCustomizableCrypto.Decrypt(compressedBuffer, (byte) pakEntry.CustomData, AesKey, pakEntry.Name)
                    : compressedBuffer;

                // Calculate the uncompressed size,
                // its either just the compression block size,
                // or if it's the last block, it's the remaining data size
                var uncompressedSize = (int) Math.Min(compressionBlockSize, pakEntry.UncompressedSize - blockIndex * compressionBlockSize);
                Compression.Compression.Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += uncompressedSize;
            }

            if (pakEntry.Extension is "luac")
                return NRCLua.DecryptLuaBytecode(pakEntry.Path, uncompressed);

            var offsetInFirstBlock = offset - firstBlockIndex * compressionBlockSize;
            if (offsetInFirstBlock == 0 && requestedSize == bufferSize)
                return uncompressed;

            var result = new byte[requestedSize];
            Array.Copy(uncompressed, offsetInFirstBlock, result, 0, requestedSize);
            return result;
        }

        // Pak Entry is written before the file data,
        // but it's the same as the one from the index, just without a name
        // We don't need to serialize that again so + file.StructSize

        var readOffset = offset & ~((long) alignment - 1);
        var dataOffset = offset - readOffset;
        var readSize = (dataOffset + requestedSize).Align(alignment);

        var data = reader.ReadBytesAt(pakEntry.Offset + pakEntry.StructSize + readOffset, (int) readSize);
        if (pakEntry.IsEncrypted)
            data = FCustomizableCrypto.Decrypt(data, (byte) pakEntry.CustomData, AesKey, pakEntry.Name);

        if (pakEntry.Extension is "luac")
            return NRCLua.DecryptLuaBytecode(pakEntry.Path, data);

        if (dataOffset == 0 && requestedSize == data.Length)
            return data;

        var chunk = new byte[requestedSize];
        Array.Copy(data, dataOffset, chunk, 0, requestedSize);
        return chunk;
    }
}
