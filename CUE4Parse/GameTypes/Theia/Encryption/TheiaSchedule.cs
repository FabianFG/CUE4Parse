using System.Buffers.Binary;

namespace CUE4Parse.GameTypes.Theia.Encryption;

public static class TheiaSchedule
{
    public static readonly byte[] GLOBAL_KEY =
    [
        0x4D, 0xEA, 0x1D, 0xC5, 0x69, 0x9F, 0x44, 0xB0, 0x86, 0x1D, 0x71, 0x5F, 0x68, 0x01, 0x7D, 0x59,
        0xC9, 0x0B, 0x51, 0x2E, 0x5B, 0xEC, 0xB5, 0x5D, 0x02, 0xE6, 0x83, 0x52, 0x34, 0x91, 0xD7, 0x19,
    ];

    public static readonly uint[] CONST8 =
    [
        0xBEDC8A41, 0xC2083AA2, 0x7A40B8E0, 0x07D37C60,
        0x16B42333, 0x5F76DE9B, 0x53881F06, 0x107B149C,
    ];

    private static readonly uint[] SHA256_IV4 = [0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A];

    private static uint RotateLeft(uint x, int n) => (x << n) | (x >> (32 - n));
    private static uint RotateRight(uint x, int n) => (x >> n) | (x << (32 - n));

    private static void Round3B40(Span<uint> s, ReadOnlySpan<uint> k)
    {
        unchecked
        {
            var r9 = s[4];
            var r10 = s[0] + r9 + k[0];
            var edx = s[8];
            var eax = r10 ^ s[12];
            var r12 = k[1];
            var r15 = k[3];
            var r8 = RotateLeft(eax, 16);
            edx = edx + r8;
            r9 = r9 ^ edx;
            var ecx = RotateLeft(r9, 20);
            r9 = s[5];
            r12 = r12 + ecx + r10;
            r10 = k[2] + s[1] + r9;
            eax = r12 ^ r8;
            r8 = RotateLeft(eax, 24);
            var tRsp70 = r8;
            eax = r8 + edx;
            var tRsp0 = eax;
            eax ^= ecx;
            ecx = RotateLeft(eax, 25);
            var tRsp4 = ecx;

            r8 = s[9];
            eax = r10 ^ s[13];
            edx = RotateLeft(eax, 16);
            r8 = r8 + edx;
            r9 ^= r8;
            ecx = RotateLeft(r9, 20);
            r9 = s[6];
            r15 = r15 + ecx + r10;
            r10 = k[4] + s[2] + r9;
            eax = r15 ^ edx;
            edx = RotateLeft(eax, 24);
            var tRsp78 = edx;
            eax = edx + r8;
            var tRsp8 = eax;
            eax ^= ecx;
            var esi = RotateLeft(eax, 25);

            edx = RotateLeft(r10 ^ s[14], 16);
            r8 = s[10] + edx;
            r9 ^= r8;
            ecx = RotateLeft(r9, 20);
            r9 = s[7];
            var r14 = k[5] + ecx + r10;
            r10 = k[6] + s[3] + r9;
            eax = r14 ^ edx;
            var r13 = RotateLeft(eax, 24);
            var r11 = r8 + r13;
            eax = r11 ^ ecx;
            var edi = RotateLeft(eax, 25);

            edx = RotateLeft(r10 ^ s[15], 16);
            r8 = s[11] + edx;
            r9 ^= r8;
            ecx = RotateLeft(r9, 20);
            var ebp = k[7] + ecx + r10;
            eax = ebp ^ edx;
            edx = RotateLeft(eax, 24);
            r10 = edx + r8;
            eax = r10 ^ ecx;
            var ebx = RotateLeft(eax, 25);

            // second half
            ecx = k[8] + r12 + esi;
            eax = ecx ^ edx;
            edx = RotateLeft(eax, 16);
            var r9w = r11 + edx;
            eax = r9w ^ esi;
            r8 = RotateLeft(eax, 20);
            ecx = ecx + k[9] + r8;
            s[0] = ecx;
            eax = RotateLeft(ecx ^ edx, 24);
            s[15] = eax;
            eax = eax + r9w;
            s[10] = eax;
            s[5] = RotateRight(eax ^ r8, 7);

            ecx = k[10] + r15 + edi;
            eax = ecx ^ tRsp70;
            edx = RotateLeft(eax, 16);
            r9w = r10 + edx;
            eax = r9w ^ edi;
            r8 = RotateLeft(eax, 20);
            ecx = ecx + k[11] + r8;
            s[1] = ecx;
            eax = RotateLeft(ecx ^ edx, 24);
            s[12] = eax;
            eax = eax + r9w;
            s[11] = eax;
            s[6] = RotateRight(eax ^ r8, 7);

            ecx = k[12] + r14 + ebx;
            r9w = tRsp0;
            eax = ecx ^ tRsp78;
            edx = RotateLeft(eax, 16);
            r9w = r9w + edx;
            eax = r9w ^ ebx;
            r8 = RotateLeft(eax, 20);
            ecx = ecx + k[13] + r8;
            s[2] = ecx;
            eax = RotateLeft(ecx ^ edx, 24);
            s[13] = eax;
            eax = eax + r9w;
            s[8] = eax;
            s[7] = RotateRight(eax ^ r8, 7);

            ecx = k[14] + ebp + tRsp4;
            r9w = tRsp8;
            eax = ecx ^ r13;
            edx = RotateLeft(eax, 16);
            r9w = r9w + edx;
            eax = r9w ^ tRsp4;
            r8 = RotateLeft(eax, 20);
            ecx = ecx + k[15] + r8;
            s[3] = ecx;
            eax = RotateLeft(ecx ^ edx, 24);
            s[14] = eax;
            eax = eax + r9w;
            s[9] = eax;
            s[4] = RotateRight(eax ^ r8, 7);
        }
    }

    private static void ShuffleKey(Span<uint> k)
    {
        Span<uint> tmp =
        [
            k[2],
            k[6],
            k[3],
            k[10],
            k[7],
            k[0],
            k[4],
            k[13],
            k[1],
            k[11],
            k[12],
            k[5],
            k[9],
            k[14],
            k[15],
            k[8],
        ];
        tmp.CopyTo(k);
    }

    public static void TheiaHash(ReadOnlySpan<uint> arg1_8, ReadOnlySpan<uint> arg2_16, ulong r8, uint r9, uint fifth, Span<byte> output)
    {
        if (arg1_8.Length < 8)
            throw new ArgumentException("arg1_8 must have 8 words", nameof(arg1_8));
        if (arg2_16.Length < 16)
            throw new ArgumentException("arg2_16 must have 16 words", nameof(arg2_16));
        if (output.Length < 64)
            throw new ArgumentException("output must be at least 64 bytes", nameof(output));

        Span<uint> state = stackalloc uint[16];
        arg1_8[..8].CopyTo(state);
        SHA256_IV4.AsSpan().CopyTo(state[8..]);
        state[12] = (uint)r8;
        state[13] = (uint)(r8 >> 32);
        state[14] = r9;
        state[15] = fifth;

        Span<uint> orig = stackalloc uint[16];
        state.CopyTo(orig);

        Span<uint> key = stackalloc uint[16];
        arg2_16[..16].CopyTo(key);

        for (var i = 0; i < 7; i++)
        {
            Round3B40(state, key);
            if (i < 6)
                ShuffleKey(key);
        }

        Span<uint> outW = stackalloc uint[16];
        unchecked
        {
            for (var i = 0; i < 8; i++)
            {
                outW[i] = state[i] ^ state[i + 8];
                outW[i + 8] = state[i + 8] ^ orig[i];
            }
        }

        for (var i = 0; i < 16; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(i * 4, 4), outW[i]);
    }

    public static byte[] TheiaHash(ReadOnlySpan<uint> arg1_8, ReadOnlySpan<uint> arg2_16, ulong r8, uint r9, uint fifth)
    {
        var output = new byte[64];
        TheiaHash(arg1_8, arg2_16, r8, r9, fifth, output);
        return output;
    }

    public static void InitPageState(ReadOnlySpan<byte> meta, int page, Span<byte> output)
    {
        if (output.Length < 33)
            throw new ArgumentException("output must be at least 33 bytes", nameof(output));

        var off = (page + 8) * 64 + 0x20;
        if (meta.Length < off + 32)
            throw new ArgumentException($"meta too small for page {page}", nameof(meta));

        var pageKey = meta.Slice(off, 32);
        Span<uint> arg2 = stackalloc uint[16];
        for (var i = 0; i < 8; i++)
            arg2[i] = BinaryPrimitives.ReadUInt32LittleEndian(GLOBAL_KEY.AsSpan(i * 4, 4));
        for (var i = 0; i < 8; i++)
            arg2[8 + i] = BinaryPrimitives.ReadUInt32LittleEndian(pageKey.Slice(i * 4, 4));

        Span<byte> digest = stackalloc byte[64];
        TheiaHash(CONST8, arg2, 0, 0x40, 0x11, digest);
        digest[..32].CopyTo(output);
        output[32] = 0x01;
    }

    public static byte[] InitPageState(ReadOnlySpan<byte> meta, int page)
    {
        var output = new byte[33];
        InitPageState(meta, page, output);
        return output;
    }

    public static void KeystreamBlock(ReadOnlySpan<byte> state33, ulong block, Span<byte> output)
    {
        if (state33.Length < 33)
            throw new ArgumentException("state33 must be 33 bytes", nameof(state33));
        if (output.Length < 64)
            throw new ArgumentException("output must be at least 64 bytes", nameof(output));

        Span<uint> arg1 = stackalloc uint[8];
        for (var i = 0; i < 8; i++)
            arg1[i] = BinaryPrimitives.ReadUInt32LittleEndian(state33.Slice(i * 4, 4));

        var sb = (uint)state33[32];
        Span<uint> zeros = stackalloc uint[16];
        zeros.Clear();
        TheiaHash(arg1, zeros, block, sb, sb ^ 0x1Bu, output);
    }

    public static byte[] KeystreamBlock(ReadOnlySpan<byte> state33, ulong block)
    {
        var output = new byte[64];
        KeystreamBlock(state33, block, output);
        return output;
    }
}
