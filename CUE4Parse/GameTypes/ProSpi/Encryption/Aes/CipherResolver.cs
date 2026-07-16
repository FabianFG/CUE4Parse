using System.Buffers.Binary;

namespace CUE4Parse.GameTypes.ProSpi.Encryption.Aes;

public static partial class ProSpiEncryption
{
    private const int Mt19937_64StateSize = 312;
    private const int Mt19937_64MiddleWord = 156;

    private const ulong Mt19937_64MatrixA = 0xB5026F5AA96619E9;
    private const ulong Mt19937_64UpperMask = 0xFFFFFFFF80000000;
    private const ulong Mt19937_64LowerMask = 0x7FFFFFFF;

    private enum KeyLayout
    {
        Full,
        First16Repeated,
        Second16Repeated,
        Swap16
    }

    private enum NonceLayout
    {
        Nonce64,
        Nonce96,
        Nonce64WithFirstWord,
        SalsaSeed012345,
        SalsaSeed234501,
        SalsaSeed345012
    }

    private enum CipherKind
    {
        ChaCha,
        ProSpiCustomPermuted,
        ProSpiCustomPermutedAlt,
        ProSpiSalsaRorPremixed,
        ProSpiCustomPermutedPremixed,
        ProSpiSalsaRor
    }

    private readonly record struct CipherSpecs
    (
        KeyLayout KeyLayout,
        NonceLayout NonceLayout,
        int NonceOffset,
        int Rounds,
        CipherKind CipherKind = CipherKind.ChaCha,
        int PremixRounds = 0
    );

    // It goes like so, trailer -> derive descriptorLookupKey (using MT19937-64) -> use it to map to descriptor -> use descriptor to choose cipher algorithm
    private static bool TryResolveCipherSpec(ReadOnlySpan<byte> trailer, out CipherSpecs spec)
    {
        spec = default;
        if (!TryGetDescriptorKey(trailer, out var descriptor))
            return false;

        return TryGetKnownCipherSpec(descriptor, out spec);
    }

    private static bool TryGetKnownCipherSpec(ulong descriptor, out CipherSpecs spec)
    {
        spec = descriptor switch
        {
            0x5E51B1FA6923008F => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce96, 12, 18),
            0xCE7E88D8DD84F42C => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 12, 12),
            0xAC7044A9A2A16B70 => new CipherSpecs(KeyLayout.First16Repeated, NonceLayout.Nonce96, 12, 20),
            0xDB8419469B3B9E4C => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 8, 10),
            0x3B846FC8D3091AEE => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 8, 8),
            0x185EB3B0ADCC5BF8 => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 16, 10),
            0xAFC4D1B9A88C5583 => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce96, 8, 10),
            0x2CEB04D65DD121E1 => new CipherSpecs(KeyLayout.Swap16, NonceLayout.Nonce96, 0, 10),
            0xE80C01FBC8BAE580 => new CipherSpecs(KeyLayout.First16Repeated, NonceLayout.Nonce64, 0, 10),
            0xD88D7AFA8A718C14 => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce96, 0, 10),
            0x18BD09EA1FF2E264 => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64WithFirstWord, 16, 14),
            0x5CFB14BDC4A27D3A => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 0, 10, CipherKind.ProSpiCustomPermuted),
            0x78BD79F475FD1FD0 => new CipherSpecs(KeyLayout.Swap16, NonceLayout.Nonce64, 0, 6, CipherKind.ProSpiCustomPermuted),
            0x3990293EF365A428 => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 8, 6, CipherKind.ProSpiCustomPermuted),
            0xCA2F4E8D213D2BEF => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 0, 8, CipherKind.ProSpiCustomPermuted),
            0xE50C453CF956F6ED => new CipherSpecs(KeyLayout.Swap16, NonceLayout.Nonce64, 0, 6, CipherKind.ProSpiCustomPermuted),
            0xF55D9E838C8CC6B5 => new CipherSpecs(KeyLayout.First16Repeated, NonceLayout.Nonce64, 0, 7, CipherKind.ProSpiCustomPermuted),
            0xB3341ED88A723037 => new CipherSpecs(KeyLayout.Second16Repeated, NonceLayout.Nonce64, 8, 6, CipherKind.ProSpiCustomPermuted),
            0x0C256B87177991D4 => new CipherSpecs(KeyLayout.First16Repeated, NonceLayout.Nonce64, 8, 4, CipherKind.ProSpiCustomPermuted),
            0x5583760930DE62E1 => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 12, 10, CipherKind.ProSpiCustomPermuted),
            0x3154B86A7EC79D9C => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 8, 5, CipherKind.ProSpiCustomPermuted),
            0x848CA23594AF2BDE => new CipherSpecs(KeyLayout.Swap16, NonceLayout.Nonce64, 8, 6, CipherKind.ProSpiCustomPermuted),
            0x161320FC4AAD784F => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 16, 7, CipherKind.ProSpiCustomPermuted),
            0xF3E438E3CCBA07E6 => new CipherSpecs(KeyLayout.Swap16, NonceLayout.Nonce64, 16, 5, CipherKind.ProSpiCustomPermuted),
            0x02D03D38AFCCF46F => new CipherSpecs(KeyLayout.Full, NonceLayout.Nonce64, 12, 6, CipherKind.ProSpiCustomPermutedAlt),
            0xC97484323BD6ADBA => new CipherSpecs(KeyLayout.Full, NonceLayout.SalsaSeed345012, 0, 8, CipherKind.ProSpiCustomPermutedPremixed, 8),
            0xF35125988F54AF14 => new CipherSpecs(KeyLayout.Swap16, NonceLayout.Nonce64, 8, 8, CipherKind.ProSpiSalsaRor),
            0x6EC058461FC7F09D => new CipherSpecs(KeyLayout.First16Repeated, NonceLayout.Nonce64, 12, 4, CipherKind.ProSpiSalsaRor),
            0x19018DB8D0B6F6C2 => new CipherSpecs(KeyLayout.Full, NonceLayout.SalsaSeed234501, 0, 4, CipherKind.ProSpiSalsaRorPremixed, 4),
            0xDFBC689E3EB61740 => new CipherSpecs(KeyLayout.Second16Repeated, NonceLayout.SalsaSeed012345, 0, 5, CipherKind.ProSpiSalsaRorPremixed, 5),
            0xF00101AAF0B9CB84 => new CipherSpecs(KeyLayout.Full, NonceLayout.SalsaSeed345012, 0, 6, CipherKind.ProSpiSalsaRorPremixed, 6),
            _ => default
        };

        return spec.Rounds != 0;
    }

    private static bool TryGetDescriptorKey(ReadOnlySpan<byte> trailer, out ulong descriptor)
    {
        descriptor = 0;

        var descriptorLookupKey = BuildDescriptorLookupKey(trailer);
        if (TryGetDescriptorFromGeneratedTable(descriptorLookupKey, out descriptor))
            return true;

        if (_missingDescriptorLookupKeys.TryAdd(descriptorLookupKey, 0))
        {
            Log.Information("ProSpi descriptor key lookup missing: descriptorLookupKey=0x{DescriptorLookupKey:X16}, trailer={Trailer}", descriptorLookupKey, Convert.ToHexString(trailer));
        }

        return false;
    }


    private static ulong BuildDescriptorLookupKey(ReadOnlySpan<byte> trailer)
    {
        Span<uint> seedInput =
        [
            BinaryPrimitives.ReadUInt32LittleEndian(trailer),
            BinaryPrimitives.ReadUInt32LittleEndian(trailer[8..]),
            BinaryPrimitives.ReadUInt32LittleEndian(trailer[16..]),
        ];

        Span<ulong> state = stackalloc ulong[Mt19937_64StateSize];
        SeedMt19937_64FromSeedSequence(state, seedInput, out var index);
        var secondSeed = NextMt19937_64(state, ref index) & 0xFFF;

        SeedMt19937_64(state, secondSeed, out index);
        return NextMt19937_64(state, ref index);
    }

    private static void SeedMt19937_64(Span<ulong> state, ulong seed, out int index)
    {
        state[0] = seed;
        for (var i = 1; i < state.Length; i++)
            state[i] = 0x5851F42D4C957F2D * (state[i - 1] ^ (state[i - 1] >> 62)) + (uint) i;

        index = Mt19937_64StateSize;
    }

    private static void SeedMt19937_64FromSeedSequence(Span<ulong> state, ReadOnlySpan<uint> seedInput, out int index)
    {
        Span<uint> generated = stackalloc uint[Mt19937_64StateSize * 2];
        GenerateSeedSequence(seedInput, generated);

        for (var i = 0; i < state.Length; i++)
            state[i] = generated[i * 2] | ((ulong) generated[i * 2 + 1] << 32);

        index = Mt19937_64StateSize;
    }

    private static void GenerateSeedSequence(ReadOnlySpan<uint> seedInput, Span<uint> output)
    {
        if (output.Length == 0)
            return;

        output.Fill(0x8B8B8B8B);
        var n = output.Length;
        var s = seedInput.Length;
        var t = n >= 623 ? 11 :
            n >= 68 ? 7 :
            n >= 39 ? 5 :
            n >= 7 ? 3 :
            (n - 1) / 2;
        var p = (n - t) / 2;
        var q = p + t;
        var m = Math.Max(s + 1, n);

        for (var k = 0; k < m; k++)
        {
            var r1 = 1664525U * SeedSeqTransform(output[k % n] ^ output[(k + p) % n] ^ output[(k + n - 1) % n]);
            output[(k + p) % n] += r1;

            var kMod = (uint) (k % n);
            var add = k == 0 ? (uint) s :
                k <= s ? seedInput[k - 1] + kMod :
                kMod;
            var r2 = r1 + add;
            output[(k + q) % n] += r2;
            output[k % n] = r2;
        }

        for (var k = m; k < m + n; k++)
        {
            var r3 = 1566083941U * SeedSeqTransform(output[k % n] + output[(k + p) % n] + output[(k + n - 1) % n]);
            output[(k + p) % n] ^= r3;

            var r4 = r3 - (uint) (k % n);
            output[(k + q) % n] ^= r4;
            output[k % n] = r4;
        }
    }

    private static uint SeedSeqTransform(uint value) => value ^ (value >> 27);

    private static ulong NextMt19937_64(Span<ulong> state, ref int index)
    {
        if (index >= Mt19937_64StateSize)
            TwistMt19937_64(state, ref index);

        var y = state[index++];
        y ^= (y >> 29) & 0x5555555555555555;
        y ^= (y << 17) & 0x71D67FFFEDA60000;
        y ^= (y << 37) & 0xFFF7EEE000000000;
        y ^= y >> 43;
        return y;
    }

    private static void TwistMt19937_64(Span<ulong> state, ref int index)
    {
        for (var i = 0; i < Mt19937_64StateSize; i++)
        {
            var x = (state[i] & Mt19937_64UpperMask) | (state[(i + 1) % Mt19937_64StateSize] & Mt19937_64LowerMask);
            var xA = x >> 1;
            if ((x & 1) != 0)
                xA ^= Mt19937_64MatrixA;
            state[i] = state[(i + Mt19937_64MiddleWord) % Mt19937_64StateSize] ^ xA;
        }

        index = 0;
    }
}
