using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Algorithms;

public class FCustomizableCryptoAlgorithmMLE : ICustomizableCryptoAlgorithm
{
    public int MaximumKeySize => 64;

    private static void ComputeBlockIndices(Span<int> indices, long keySeed, ReadOnlySpan<byte> key)
    {
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        var indicesKey = XxHash3.HashToUInt64(key, keySeed);
        if (indicesKey == 0)
            indicesKey = 0x9E3779B97F4A7C15;

        for (int i = indices.Length - 1; i > 0; i--)
        {
            var tmp = indicesKey ^ (indicesKey << 13) ^ ((indicesKey ^ (indicesKey << 13)) >> 7);
            indicesKey = tmp ^ (tmp << 17);

            var swapIndex0 = (int)(indicesKey % (uint) (i + 1));
            (indices[swapIndex0], indices[i]) = (indices[i], indices[swapIndex0]);
        }
    }

    public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key)
    {
        ReadOnlySpan<byte> keySpan = key.Span;
        Span<byte> ciphertextSpan = ciphertext.AsSpan();

        var decryptedData = new byte[ciphertextSpan.Length];
        Span<byte> decryptedSpan = decryptedData.AsSpan();

        var keySeed = (long)XxHash3.HashToUInt64(keySpan);
        var calculatedBlockSize = 1 << (int) (Math.Log2(Math.Sqrt(ciphertextSpan.Length)) + 2);

        var minimumBlockSize = Math.Max(0x10, calculatedBlockSize);
        var blockSize = Math.Min(0x1000, minimumBlockSize);
        var blockHashPartSize = Math.Min(0x40, blockSize);

        var totalSize = ciphertextSpan.Length + blockSize - 1;
        var totalBlockCount = totalSize / blockSize;
        var fullBlockCount = totalBlockCount - 1;

        var blockIndices = new int[fullBlockCount];
        ComputeBlockIndices(blockIndices, keySeed, keySpan);

        var keys = new ulong[totalBlockCount];
        {
            var xxh = new XxHash3();

            xxh.Append(keySpan);
            xxh.Append(MemoryMarshal.Cast<long, byte>(new ReadOnlySpan<long>(ref keySeed)));

            keys[0] = xxh.GetCurrentHashAsUInt64();
        }

        for (int i = 0; i < blockIndices.Length; i++)
        {
            var index = i == fullBlockCount
                ? fullBlockCount
                : blockIndices[i];

            var offset = blockSize * index;
            keys[i + 1] = XxHash3.HashToUInt64(ciphertextSpan.Slice(offset, blockHashPartSize));
        }

        for (int i = 0; i < totalBlockCount; i++)
        {
            int currentBlockSize;
            int blockIndex;

            if (i == fullBlockCount)
            {
                currentBlockSize = ciphertextSpan.Length - fullBlockCount * blockSize;
                blockIndex = fullBlockCount;
            }
            else
            {
                currentBlockSize = blockSize;
                blockIndex = blockIndices[i];
            }

            var blockKey = keys[i];
            if (blockKey == 0)
                blockKey = 0x9E3779B97F4A7C15;

            Span<byte> encryptedBlockData = ciphertextSpan.Slice(blockIndex * blockSize, currentBlockSize);
            Span<byte> decryptedBlockData = decryptedSpan.Slice(i * blockSize, currentBlockSize);

            for (int blockOffset = 0; blockOffset < currentBlockSize; blockOffset += 8)
            {
                var tmp = blockKey ^ (blockKey << 13) ^ ((blockKey ^ (blockKey << 13)) >> 7);
                blockKey = tmp ^ (tmp << 17);

                var currentChunkSize = Math.Min(8, currentBlockSize - blockOffset);
                for (int chunkOffset = 0; chunkOffset < currentChunkSize; chunkOffset++)
                {
                    var currentByte = encryptedBlockData[blockOffset + chunkOffset];
                    currentByte ^= (byte) (blockKey >> (8 * chunkOffset));
                    decryptedBlockData[blockOffset + chunkOffset] = currentByte;
                }
            }
        }

        return decryptedData;
    }
}
