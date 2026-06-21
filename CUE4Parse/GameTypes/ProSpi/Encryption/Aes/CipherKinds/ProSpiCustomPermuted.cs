using System.Buffers.Binary;
using System.Numerics;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static void ProSpiCustomPermutedXor(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[ChaChaBlockSize];
        BuildProSpiCustomPermutedState(state, trailer, spec);

        for (var offset = 0; offset < payload.Length; offset += ChaChaBlockSize)
        {
            ProSpiCustomPermutedBlock(state, spec.Rounds, block);
            var blockSize = Math.Min(ChaChaBlockSize, payload.Length - offset);
            for (var i = 0; i < blockSize; i++)
                payload[offset + i] ^= block[i];

            if (++state[8] == 0)
                state[5]++;
        }
    }

    private static void BuildProSpiCustomPermutedState(Span<uint> state, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> key = stackalloc uint[8];
        for (var i = 0; i < key.Length; i++)
            key[i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(i * sizeof(uint), sizeof(uint)));

        var trailerWordOffset = spec.NonceOffset / sizeof(uint);
        var nonce0 = ReadTrailerWord(trailer, trailerWordOffset);
        var nonce1 = ReadTrailerWord(trailer, trailerWordOffset + 1);

        state[0] = 0x61707865;
        state[1] = 0x3120646E;
        state[2] = 0x79622D36;
        state[3] = 0x6B206574;
        switch (spec.KeyLayout)
        {
            case KeyLayout.Swap16:
                state[4] = key[7];
                state[5] = 0;
                state[6] = key[3];
                state[7] = key[6];
                state[8] = 0;
                state[9] = key[2];
                state[10] = key[5];
                state[11] = nonce1;
                state[12] = key[1];
                state[13] = key[4];
                state[14] = nonce0;
                state[15] = key[0];
                break;
            case KeyLayout.Second16Repeated:
                state[4] = key[7];
                state[5] = 0;
                state[6] = key[7];
                state[7] = key[6];
                state[8] = 0;
                state[9] = key[6];
                state[10] = key[5];
                state[11] = nonce1;
                state[12] = key[5];
                state[13] = key[4];
                state[14] = nonce0;
                state[15] = key[4];
                break;
            case KeyLayout.First16Repeated:
                state[4] = key[3];
                state[5] = 0;
                state[6] = key[3];
                state[7] = key[2];
                state[8] = 0;
                state[9] = key[2];
                state[10] = key[1];
                state[11] = nonce1;
                state[12] = key[1];
                state[13] = key[0];
                state[14] = nonce0;
                state[15] = key[0];
                break;
            default:
                state[4] = key[3];
                state[5] = 0;
                state[6] = key[7];
                state[7] = key[2];
                state[8] = 0;
                state[9] = key[6];
                state[10] = key[1];
                state[11] = nonce1;
                state[12] = key[5];
                state[13] = key[0];
                state[14] = nonce0;
                state[15] = key[4];
                break;
        }
    }

    private static void ProSpiCustomPermutedBlock(ReadOnlySpan<uint> state, int rounds, Span<byte> block)
    {
        var v9 = state[8];
        var v10 = state[1];
        var v11 = state[2];
        var v64 = state[3];
        var v63 = state[4];
        var v62 = state[5];
        var v12 = state[6];
        var v58 = state[7];
        var v13 = state[9];
        var v59 = state[10];
        var v57 = state[11];
        var v14 = state[12];
        var v60 = state[14];
        var v15 = state[15];
        var v65 = state[13];
        var v61 = v65;
        var v56 = state[0];
        var v17 = state[0];

        for (var round = 0; round < rounds; round++)
        {
            var v18 = v63 ^ BitOperations.RotateRight(v14 + v17, 25);
            var v48 = v9 ^ BitOperations.RotateRight(v18 + v17, 23);
            var v47 = v14 ^ BitOperations.RotateRight(v48 + v18, 19);
            var v19 = v17 ^ BitOperations.RotateRight(v48 + v47, 14);
            var v20 = v62 ^ BitOperations.RotateRight(v10 + v61, 25);
            var v49 = v13 ^ BitOperations.RotateRight(v20 + v10, 23);
            var v21 = v61 ^ BitOperations.RotateRight(v49 + v20, 19);
            var v22 = v10 ^ BitOperations.RotateRight(v49 + v21, 14);
            var v50 = v12 ^ BitOperations.RotateRight(v11 + v60, 25);
            var v23 = v59 ^ BitOperations.RotateRight(v50 + v11, 23);
            var v24 = v60 ^ BitOperations.RotateRight(v23 + v50, 19);
            var v25 = v11 ^ BitOperations.RotateRight(v24 + v23, 14);
            var v26 = v58 ^ BitOperations.RotateRight(v64 + v15, 25);
            var v27 = v57 ^ BitOperations.RotateRight(v26 + v64, 23);
            var v28 = v15 ^ BitOperations.RotateRight(v27 + v26, 19);
            var v29 = v64 ^ BitOperations.RotateRight(v28 + v27, 14);
            var v30 = v21 ^ BitOperations.RotateRight(v19 + v26, 25);
            var v31 = v23 ^ BitOperations.RotateRight(v30 + v19, 23);
            v61 = v30;
            v58 = v26 ^ BitOperations.RotateRight(v31 + v30, 19);
            v59 = v31;
            v17 = v19 ^ BitOperations.RotateRight(v58 + v31, 14);
            v60 = v24 ^ BitOperations.RotateRight(v22 + v18, 25);
            v57 = v27 ^ BitOperations.RotateRight(v60 + v22, 23);
            v63 = v18 ^ BitOperations.RotateRight(v57 + v60, 19);
            v10 = v22 ^ BitOperations.RotateRight(v57 + v63, 14);
            v15 = v28 ^ BitOperations.RotateRight(v25 + v20, 25);
            v9 = v48 ^ BitOperations.RotateRight(v15 + v25, 23);
            v62 = v20 ^ BitOperations.RotateRight(v9 + v15, 19);
            v11 = v25 ^ BitOperations.RotateRight(v9 + v62, 14);
            v14 = v47 ^ BitOperations.RotateRight(v29 + v50, 25);
            v13 = v49 ^ BitOperations.RotateRight(v14 + v29, 23);
            v12 = v50 ^ BitOperations.RotateRight(v13 + v14, 19);
            v64 = v29 ^ BitOperations.RotateRight(v12 + v13, 14);
        }

        Span<uint> output =
        [
            v56 + v17,
            v61 + state[13],
            state[10] + v59,
            state[7] + v58,
            state[4] + v63,
            state[1] + v10,
            state[14] + v60,
            state[11] + v57,
            state[8] + v9,
            state[5] + v62,
            state[2] + v11,
            state[15] + v15,
            state[12] + v14,
            state[9] + v13,
            state[6] + v12,
            state[3] + v64,
        ];

        for (var i = 0; i < output.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(block.Slice(i * sizeof(uint), sizeof(uint)), output[i]);
    }
}
