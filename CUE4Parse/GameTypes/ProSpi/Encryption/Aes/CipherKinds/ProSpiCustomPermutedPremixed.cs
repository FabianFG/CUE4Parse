using System.Buffers.Binary;
using System.Numerics;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static void ProSpiCustomPermutedPremixedXor(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[ChaChaBlockSize];
        BuildProSpiCustomPermutedPremixedState(state, trailer, spec);

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

    private static void BuildProSpiCustomPermutedPremixedState(Span<uint> state, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> key = stackalloc uint[8];
        for (var i = 0; i < key.Length; i++)
            key[i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(i * sizeof(uint), sizeof(uint)));

        Span<uint> seed = stackalloc uint[6];
        if (spec.NonceLayout == NonceLayout.SalsaSeed345012)
        {
            seed[0] = ReadTrailerWord(trailer, 3);
            seed[1] = ReadTrailerWord(trailer, 4);
            seed[2] = ReadTrailerWord(trailer, 5);
            seed[3] = ReadTrailerWord(trailer, 0);
            seed[4] = ReadTrailerWord(trailer, 1);
            seed[5] = ReadTrailerWord(trailer, 2);
        }
        else
        {
            for (var i = 0; i < seed.Length; i++)
                seed[i] = ReadTrailerWord(trailer, i);
        }

        state[0] = 0x61707865;
        state[1] = 0x3120646E;
        state[2] = 0x79622D36;
        state[3] = 0x6B206574;
        state[4] = key[3];
        state[5] = 0;
        state[6] = key[7];
        state[7] = key[2];
        state[8] = 0;
        state[9] = key[6];
        state[10] = key[1];
        state[11] = seed[5];
        state[12] = key[5];
        state[13] = key[0];
        state[14] = seed[4];
        state[15] = key[4];

        ProSpiCustomPermutedPremixState(state, seed, spec.PremixRounds);
    }

    private static void ProSpiCustomPermutedPremixState(Span<uint> state, ReadOnlySpan<uint> seed, int rounds)
    {
        var v33 = seed[0];
        var v75 = seed[1];
        var v34 = seed[2];
        var v35 = seed[3];
        state[14] = seed[4];
        state[11] = seed[5];

        var v70 = state[13];
        var v69 = state[10];
        var v71 = state[7];
        var v36 = state[4];
        var v37 = state[15];
        var v38 = state[12];
        var v39 = state[9];
        var v76 = state[6];
        var v40 = state[0];
        var v41 = state[1];
        var v74 = state[2];
        var v42 = state[3];
        var v43 = v33;

        for (var round = 0; round < rounds; round++)
        {
            var v68 = v36 ^ BitOperations.RotateRight(v38 + v40, 25);
            var v65 = v34 ^ BitOperations.RotateRight(v68 + v40, 23);
            var v64 = v38 ^ BitOperations.RotateRight(v65 + v68, 19);
            var v45 = v40 ^ BitOperations.RotateRight(v65 + v64, 14);
            var v67 = v35 ^ BitOperations.RotateRight(v70 + v41, 25);
            var v66 = v39 ^ BitOperations.RotateRight(v67 + v41, 23);
            var v46 = v70 ^ BitOperations.RotateRight(v66 + v67, 19);
            var v47 = v41 ^ BitOperations.RotateRight(v46 + v66, 14);
            var v77 = v76 ^ BitOperations.RotateRight(v43 + v74, 25);
            var v48 = v69 ^ BitOperations.RotateRight(v77 + v74, 23);
            var v49 = v43 ^ BitOperations.RotateRight(v48 + v77, 19);
            var v50 = v74 ^ BitOperations.RotateRight(v49 + v48, 14);
            var v51 = v71 ^ BitOperations.RotateRight(v37 + v42, 25);
            var v53 = v75 ^ BitOperations.RotateRight(v51 + v42, 23);
            var v54 = v37 ^ BitOperations.RotateRight(v53 + v51, 19);
            var v55 = v42 ^ BitOperations.RotateRight(v54 + v53, 14);
            v70 = v46 ^ BitOperations.RotateRight(v45 + v51, 25);
            v69 = v48 ^ BitOperations.RotateRight(v70 + v45, 23);
            v71 = v51 ^ BitOperations.RotateRight(v69 + v70, 19);
            v40 = v45 ^ BitOperations.RotateRight(v71 + v69, 14);
            var v56 = v49 ^ BitOperations.RotateRight(v47 + v68, 25);
            var v57 = v53 ^ BitOperations.RotateRight(v56 + v47, 23);
            v43 = v56;
            v36 = v68 ^ BitOperations.RotateRight(v57 + v56, 19);
            v75 = v57;
            var v58 = v47 ^ BitOperations.RotateRight(v36 + v57, 14);
            v37 = v54 ^ BitOperations.RotateRight(v50 + v67, 25);
            v34 = v65 ^ BitOperations.RotateRight(v37 + v50, 23);
            v35 = v67 ^ BitOperations.RotateRight(v34 + v37, 19);
            v74 = v50 ^ BitOperations.RotateRight(v35 + v34, 14);
            v38 = v64 ^ BitOperations.RotateRight(v77 + v55, 25);
            v39 = v66 ^ BitOperations.RotateRight(v38 + v55, 23);
            v76 = v77 ^ BitOperations.RotateRight(v39 + v38, 19);
            v42 = v55 ^ BitOperations.RotateRight(v39 + v76, 14);
            v41 = v58;
        }

        state[13] = v40;
        state[10] = v41;
        state[7] = v74;
        state[4] = v42;
        state[15] = v43;
        state[12] = v75;
        state[9] = v34;
        state[6] = v35;
        state[5] = 0;
        state[8] = 0;
    }
}
