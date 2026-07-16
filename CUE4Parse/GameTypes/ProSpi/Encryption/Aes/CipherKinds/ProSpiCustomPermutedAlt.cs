using System.Buffers.Binary;
using System.Numerics;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static void ProSpiCustomPermutedAltXor(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[ChaChaBlockSize];
        BuildProSpiCustomPermutedState(state, trailer, spec);

        for (var offset = 0; offset < payload.Length; offset += ChaChaBlockSize)
        {
            ProSpiCustomPermutedAltBlock(state, spec.Rounds, block);
            var blockSize = Math.Min(ChaChaBlockSize, payload.Length - offset);
            for (var i = 0; i < blockSize; i++)
                payload[offset + i] ^= block[i];

            if (++state[8] == 0)
                state[5]++;
        }
    }

    private static void ProSpiCustomPermutedAltBlock(ReadOnlySpan<uint> state, int rounds, Span<byte> block)
    {
        var v9 = state[8];
        var v87 = state[1];
        var v11 = state[2];
        var v12 = state[3];
        var v13 = state[4];
        var v14 = state[5];
        var v89 = state[6];
        var v84 = state[7];
        var v15 = state[9];
        var v85 = state[10];
        var v83 = state[11];
        var v16 = state[12];
        var v88 = state[13];
        var v18 = state[14];
        var v86 = state[15];
        var v90 = state[13];
        var v82 = state[0];
        var v20 = state[0];

        for (var round = 0; round < rounds; round++)
        {
            var v21 = v13 ^ BitOperations.RotateRight(v16 + v20, 25);
            var v74 = v9 ^ BitOperations.RotateRight(v21 + v20, 23);
            var v73 = v16 ^ BitOperations.RotateRight(v74 + v21, 19);
            var v22 = v20 ^ BitOperations.RotateRight(v74 + v73, 14);
            var v76 = v14 ^ BitOperations.RotateRight(v87 + v88, 25);
            var v75 = v15 ^ BitOperations.RotateRight(v76 + v87, 23);
            var v23 = v88 ^ BitOperations.RotateRight(v75 + v76, 19);
            var v24 = v87 ^ BitOperations.RotateRight(v75 + v23, 14);
            var v25 = v89 ^ BitOperations.RotateRight(v11 + v18, 25);
            var v26 = v85 ^ BitOperations.RotateRight(v25 + v11, 23);
            var v28 = v18 ^ BitOperations.RotateRight(v26 + v25, 19);
            var v29 = v11 ^ BitOperations.RotateRight(v28 + v26, 14);
            var v30 = v84 ^ BitOperations.RotateRight(v12 + v86, 25);
            var v31 = v83 ^ BitOperations.RotateRight(v30 + v12, 23);
            var v32 = v86 ^ BitOperations.RotateRight(v31 + v30, 19);
            var v33 = v12 ^ BitOperations.RotateRight(v32 + v31, 14);
            var v34 = v23 ^ BitOperations.RotateRight(v22 + v30, 25);
            var v35 = v26 ^ BitOperations.RotateRight(v34 + v22, 23);
            v88 = v34;
            v84 = v30 ^ BitOperations.RotateRight(v35 + v34, 19);
            v85 = v35;
            v20 = v22 ^ BitOperations.RotateRight(v84 + v35, 14);
            var v36 = v28 ^ BitOperations.RotateRight(v24 + v21, 25);
            var v37 = v31 ^ BitOperations.RotateRight(v36 + v24, 23);
            v18 = v36;
            v13 = v21 ^ BitOperations.RotateRight(v37 + v36, 19);
            v83 = v37;
            v87 = v24 ^ BitOperations.RotateRight(v13 + v37, 14);
            var v38 = v32 ^ BitOperations.RotateRight(v29 + v76, 25);
            v9 = v74 ^ BitOperations.RotateRight(v38 + v29, 23);
            v86 = v38;
            v14 = v76 ^ BitOperations.RotateRight(v9 + v38, 19);
            v11 = v29 ^ BitOperations.RotateRight(v9 + v14, 14);
            v16 = v73 ^ BitOperations.RotateRight(v33 + v25, 25);
            v15 = v75 ^ BitOperations.RotateRight(v16 + v33, 23);
            v89 = v25 ^ BitOperations.RotateRight(v15 + v16, 19);
            v12 = v33 ^ BitOperations.RotateRight(v15 + v89, 14);
        }

        Span<uint> output =
        [
            v82 + v20,
            v88 + v90,
            state[10] + v85,
            state[7] + v84,
            state[4] + v13,
            state[1] + v87,
            state[14] + v18,
            state[11] + v83,
            state[8] + v9,
            state[5] + v14,
            state[2] + v11,
            state[15] + v86,
            state[12] + v16,
            state[9] + v15,
            state[6] + v89,
            state[3] + v12,
        ];

        for (var i = 0; i < output.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(block.Slice(i * sizeof(uint), sizeof(uint)), output[i]);
    }
}
