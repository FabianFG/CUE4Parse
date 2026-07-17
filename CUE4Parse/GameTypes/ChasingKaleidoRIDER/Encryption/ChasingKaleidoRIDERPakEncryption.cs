using System.Buffers.Binary;
using System.Numerics;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.ChasingKaleidoRIDER.Encryption;

public static class ChasingKaleidoRIDERPakEncryption
{
    private const int BlockSize = 64;

    public static byte[] Decrypt(byte[] bytes, int beginOffset, int count, bool isIndex, long absoluteOffset,
        long encryptionBaseOffset, IAesVfsReader reader)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(beginOffset, bytes.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, bytes.Length - beginOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(absoluteOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(encryptionBaseOffset);
        ArgumentOutOfRangeException.ThrowIfLessThan(absoluteOffset, encryptionBaseOffset);

        var key = reader.AesKey?.Key ?? throw new NullReferenceException("reader.AesKey");
        if (key.Length != 32)
            throw new ArgumentException("ChaCha key must be 32 bytes long", nameof(reader));

        var streamOffset = (ulong) (absoluteOffset - encryptionBaseOffset);
        var counter = streamOffset / BlockSize;
        var blockOffset = (int) (streamOffset % BlockSize);

        var output = new byte[count];
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[BlockSize];
        InitializeState(state, key, (ulong) encryptionBaseOffset, counter);

        var outputOffset = 0;
        while (outputOffset < count)
        {
            GenerateBlock(state, block);
            var blockLength = Math.Min(BlockSize - blockOffset, count - outputOffset);
            for (var i = 0; i < blockLength; i++)
                output[outputOffset + i] = (byte) (bytes[beginOffset + outputOffset + i] ^ block[blockOffset + i]);

            outputOffset += blockLength;
            blockOffset = 0;
            if (++state[12] == 0)
                state[13]++;
        }

        return output;
    }

    private static void InitializeState(Span<uint> state, ReadOnlySpan<byte> key, ulong nonce, ulong counter)
    {
        state[0] = 0x61707865;
        state[1] = 0x3320646E;
        state[2] = 0x79622D32;
        state[3] = 0x6B206574;

        for (var i = 0; i < 8; i++)
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * sizeof(uint), sizeof(uint)));

        state[12] = (uint) counter;
        state[13] = (uint) (counter >> 32);
        state[14] = (uint) nonce;
        state[15] = (uint) (nonce >> 32);
    }

    private static void GenerateBlock(ReadOnlySpan<uint> state, Span<byte> block)
    {
        Span<uint> working = stackalloc uint[16];
        state.CopyTo(working);

        for (var i = 0; i < 12; i += 2)
        {
            QuarterRound(working, 0, 4, 8, 12);
            QuarterRound(working, 1, 5, 9, 13);
            QuarterRound(working, 2, 6, 10, 14);
            QuarterRound(working, 3, 7, 11, 15);
            QuarterRound(working, 0, 5, 10, 15);
            QuarterRound(working, 1, 6, 11, 12);
            QuarterRound(working, 2, 7, 8, 13);
            QuarterRound(working, 3, 4, 9, 14);
        }

        for (var i = 0; i < working.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(block.Slice(i * sizeof(uint), sizeof(uint)), working[i] + state[i]);
    }

    private static void QuarterRound(Span<uint> state, int a, int b, int c, int d)
    {
        state[a] += state[b];
        state[d] = BitOperations.RotateLeft(state[d] ^ state[a], 16);
        state[c] += state[d];
        state[b] = BitOperations.RotateLeft(state[b] ^ state[c], 12);
        state[a] += state[b];
        state[d] = BitOperations.RotateLeft(state[d] ^ state[a], 8);
        state[c] += state[d];
        state[b] = BitOperations.RotateLeft(state[b] ^ state[c], 7);
    }
}
