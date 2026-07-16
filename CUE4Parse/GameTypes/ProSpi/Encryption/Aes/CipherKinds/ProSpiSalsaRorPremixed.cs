using System.Buffers.Binary;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private static void ProSpiSalsaRorPremixedXor(Span<byte> payload, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> state = stackalloc uint[16];
        Span<byte> block = stackalloc byte[ChaChaBlockSize];
        BuildProSpiSalsaRorPremixedState(state, trailer, spec);

        for (var offset = 0; offset < payload.Length; offset += ChaChaBlockSize)
        {
            ProSpiSalsaRorBlock(state, spec.Rounds, block);
            var blockSize = Math.Min(ChaChaBlockSize, payload.Length - offset);
            for (var i = 0; i < blockSize; i++)
                payload[offset + i] ^= block[i];

            if (++state[12] == 0)
                state[13]++;
        }
    }

    private static void BuildProSpiSalsaRorPremixedState(Span<uint> state, ReadOnlySpan<byte> trailer, CipherSpecs spec)
    {
        Span<uint> key = stackalloc uint[8];
        for (var i = 0; i < key.Length; i++)
            key[i] = BinaryPrimitives.ReadUInt32LittleEndian(_aesKey.AsSpan(i * sizeof(uint), sizeof(uint)));

        state[0] = 0x61707865;
        state[1] = 0x3120646E;
        state[2] = 0x79622D36;
        state[3] = 0x6B206574;

        switch (spec.KeyLayout)
        {
            case KeyLayout.Second16Repeated:
            {
                for (var i = 0; i < 4; i++)
                {
                    state[4 + i] = key[4 + i];
                    state[8 + i] = key[4 + i];
                }

                break;
            }

            default:
            {
                for (var i = 0; i < 8; i++)
                    state[4 + i] = key[i];
                break;
            }
        }

        Span<uint> seed = stackalloc uint[6];
        switch (spec.NonceLayout)
        {
            case NonceLayout.SalsaSeed234501:
                seed[0] = ReadTrailerWord(trailer, 2);
                seed[1] = ReadTrailerWord(trailer, 3);
                seed[2] = ReadTrailerWord(trailer, 4);
                seed[3] = ReadTrailerWord(trailer, 5);
                seed[4] = ReadTrailerWord(trailer, 0);
                seed[5] = ReadTrailerWord(trailer, 1);
                break;
            case NonceLayout.SalsaSeed345012:
                seed[0] = ReadTrailerWord(trailer, 3);
                seed[1] = ReadTrailerWord(trailer, 4);
                seed[2] = ReadTrailerWord(trailer, 5);
                seed[3] = ReadTrailerWord(trailer, 0);
                seed[4] = ReadTrailerWord(trailer, 1);
                seed[5] = ReadTrailerWord(trailer, 2);
                break;
            default:
            {
                for (var i = 0; i < seed.Length; i++)
                    seed[i] = ReadTrailerWord(trailer, i);
                break;
            }
        }

        state[12] = seed[0];
        state[13] = seed[1];
        state[14] = seed[2];
        state[15] = seed[3];

        Span<uint> mixed = stackalloc uint[16];
        ProSpiSalsaRorCore(state, spec.PremixRounds, mixed);

        state[0] = 0x61707865;
        state[1] = 0x3120646E;
        state[2] = 0x79622D36;
        state[3] = 0x6B206574;
        state[4] = mixed[0];
        state[5] = mixed[1];
        state[6] = mixed[2];
        state[7] = mixed[3];
        state[8] = mixed[12];
        state[9] = mixed[13];
        state[10] = mixed[14];
        state[11] = mixed[15];
        state[12] = 0;
        state[13] = 0;
        state[14] = seed[4];
        state[15] = seed[5];
    }
}
