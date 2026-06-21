using System.Buffers.Binary;
using System.Numerics;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private const int ChaChaBlockSize = 64;

    private static void ProSpiChaCha(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[ChaChaBlockSize];
        BuildChaChaState(state, trailer, spec);

        for (var offset = 0; offset < payload.Length; offset += ChaChaBlockSize)
        {
            ChaChaBlock(state, spec.Rounds, block);
            var blockSize = Math.Min(ChaChaBlockSize, payload.Length - offset);
            for (var i = 0; i < blockSize; i++)
                payload[offset + i] ^= block[i];

            if (++state[12] == 0)
                state[13]++;
        }
    }

    private static void BuildChaChaState(Span<uint> state, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        state[0] = 0x61707865;
        state[1] = 0x3120646E;
        state[2] = 0x79622D36;
        state[3] = 0x6B206574;

        var firstHalfOffset = spec.KeyLayout switch
        {
            KeyLayout.Swap16 or KeyLayout.Second16Repeated => 16,
            _ => 0
        };
        var secondHalfOffset = spec.KeyLayout switch
        {
            KeyLayout.First16Repeated => 0,
            KeyLayout.Second16Repeated => 16,
            KeyLayout.Swap16 => 0,
            _ => 16
        };

        for (var i = 0; i < 4; i++)
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(firstHalfOffset + i * sizeof(uint), sizeof(uint)));
        for (var i = 0; i < 4; i++)
            state[8 + i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(secondHalfOffset + i * sizeof(uint), sizeof(uint)));

        state[12] = 0;
        state[13] = 0;
        var trailerWordOffset = spec.NonceOffset / sizeof(uint);
        switch (spec.NonceLayout)
        {
            case NonceLayout.Nonce96:
                state[13] = ReadTrailerWord(trailer, trailerWordOffset);
                state[14] = ReadTrailerWord(trailer, trailerWordOffset + 1);
                state[15] = ReadTrailerWord(trailer, trailerWordOffset + 2);
                break;
            case NonceLayout.Nonce64WithFirstWord:
                state[13] = ReadTrailerWord(trailer, trailerWordOffset);
                state[14] = ReadTrailerWord(trailer, trailerWordOffset + 1);
                state[15] = ReadTrailerWord(trailer, 0);
                break;
            default:
                state[14] = ReadTrailerWord(trailer, trailerWordOffset);
                state[15] = ReadTrailerWord(trailer, trailerWordOffset + 1);
                break;
        }
    }

    private static void ChaChaBlock(ReadOnlySpan<uint> state, int rounds, Span<byte> block)
    {
        Span<uint> working = stackalloc uint[16];
        state.CopyTo(working);

        for (var i = 0; i < rounds; i += 2)
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

    private static void QuarterRound(Span<uint> x, int a, int b, int c, int d)
    {
        x[a] += x[b];
        x[d] = BitOperations.RotateLeft(x[d] ^ x[a], 16);
        x[c] += x[d];
        x[b] = BitOperations.RotateLeft(x[b] ^ x[c], 12);
        x[a] += x[b];
        x[d] = BitOperations.RotateLeft(x[d] ^ x[a], 8);
        x[c] += x[d];
        x[b] = BitOperations.RotateLeft(x[b] ^ x[c], 7);
    }
}
