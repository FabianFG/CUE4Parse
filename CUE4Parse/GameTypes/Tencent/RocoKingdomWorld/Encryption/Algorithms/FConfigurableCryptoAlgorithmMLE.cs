using System.IO.Hashing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Algorithms;

public class FConfigurableCryptoAlgorithmMLE : IConfigurableCryptoAlgorithm
{
    private static void ComputeBlockIndices(Span<int> indices, ulong keySeed, ReadOnlySpan<byte> key)
    {
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        var indicesKey = XxHash3.HashToUInt64(key, (long)keySeed);
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

    private static void DecryptSlice(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> key, Span<byte> plaintext)
    {
        var keySeed = XxHash3.HashToUInt64(key);

        var calculatedBlockSize = (int) BitOperations.RoundUpToPowerOf2((uint) Math.Ceiling(Math.Sqrt(ciphertext.Length))) << 1;
        calculatedBlockSize = Math.Min(calculatedBlockSize, 0x1000 << 1);

        var minimumBlockSize = Math.Max(0x10, calculatedBlockSize);
        var blockSize = Math.Min(0x1000, minimumBlockSize);

        DecryptSlice(ciphertext, key, plaintext, blockSize, keySeed);
    }

    public static void DecryptSlice(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> key, Span<byte> plaintext,
        int blockSize, ulong keySeed)
    {
        var blockHashPartSize = Math.Min(0x40, blockSize);

        var totalSize = ciphertext.Length + blockSize - 1;
        var totalBlockCount = totalSize / blockSize;
        var fullBlockCount = totalBlockCount - 1;

        var blockIndices = new int[fullBlockCount];
        ComputeBlockIndices(blockIndices, keySeed, key);

        var keys = new ulong[totalBlockCount];
        {
            var xxh = new XxHash3();

            xxh.Append(key);
            xxh.Append(MemoryMarshal.Cast<ulong, byte>(new ReadOnlySpan<ulong>(ref keySeed)));

            keys[0] = xxh.GetCurrentHashAsUInt64();
        }

        for (int i = 0; i < blockIndices.Length; i++)
        {
            var index = i == fullBlockCount
                ? fullBlockCount
                : blockIndices[i];

            var offset = blockSize * index;
            keys[i + 1] = XxHash3.HashToUInt64(ciphertext.Slice(offset, blockHashPartSize));
        }

        for (int i = 0; i < totalBlockCount; i++)
        {
            int currentBlockSize;
            int blockIndex;

            if (i == fullBlockCount)
            {
                currentBlockSize = ciphertext.Length - fullBlockCount * blockSize;
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

            ReadOnlySpan<byte> encryptedBlockData = ciphertext.Slice(blockIndex * blockSize, currentBlockSize);
            Span<byte> decryptedBlockData = plaintext.Slice(i * blockSize, currentBlockSize);

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
    }

    public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key)
    {
        Span<byte> ciphertextSpan = ciphertext.AsSpan();
        ReadOnlySpan<byte> keySpan = key.Span;

        var decryptedData = new byte[ciphertextSpan.Length];
        Span<byte> decryptedSpan = decryptedData.AsSpan();

        DecryptSlice(ciphertextSpan, keySpan, decryptedSpan);
        return decryptedData;
    }

    public byte[] DecryptChunked(byte[] ciphertext, ReadOnlyMemory<byte> key)
    {
        const int encryptionBlockSize = 0x1000;

        Span<byte> ciphertextSpan = ciphertext.AsSpan();
        ReadOnlySpan<byte> keySpan = key.Span;

        var decryptedData = new byte[ciphertextSpan.Length];
        Span<byte> decryptedSpan = decryptedData.AsSpan();

        var currentOffset = 0;
        while (currentOffset != ciphertextSpan.Length)
        {
            var currentBlockSize = Math.Min(ciphertext.Length - currentOffset, encryptionBlockSize);

            Span<byte> currentEncrypted = ciphertextSpan.Slice(currentOffset, currentBlockSize);
            Span<byte> currentDecrypted = decryptedSpan.Slice(currentOffset, currentBlockSize);
            DecryptSlice(currentEncrypted, keySpan, currentDecrypted);

            currentOffset += currentBlockSize;
        }

        return decryptedData;
    }
}
