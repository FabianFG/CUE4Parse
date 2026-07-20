
namespace CUE4Parse.UE4.Assets.Exports.Texture;

/// <summary>
/// BC7Prep decoder (managed implementation).
/// Oodle Texture BC7Prep is a reversible bit rearrangement of BC7 blocks, not an LZ
/// compression scheme. This is a scalar (non-SIMD) port of the reference decode logic.
/// Reference: Oodle SDK 2.9.14 bc7prep_decode.cpp (Epic Games, Inc.), used under the
/// Unreal Engine EULA to understand the on-disk format; this is an independent
/// reimplementation, not a copy of RAD/Epic's source.
/// </summary>
public static class BC7PrepDecoder
{

    private const int ModeCount = 10;
    private const uint FlagSplit0 = 1;
    private const uint FlagSwitchColorspace = 1 << 16;

    private static readonly int[] ModeSizes = [16, 16, 16, 16, 16, 16, 16, 16, 16, 4];

    // Raw per-mode split byte offsets (BC7PREP_MODEx_SPLIT constants). 0 means "never split".
    private static readonly int[] SplitPoint = [8, 8, 12, 12, 6, 8, 8, 12, 0, 0];

    private delegate void ModeDecoderFn(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace);

    private static readonly ModeDecoderFn?[] Decoders =
    {
        UnMungeMode0, UnMungeMode1, UnMungeMode2, UnMungeMode3, UnMungeMode4,
        UnMungeMode5, UnMungeMode6, UnMungeMode7, null /* mode8 special-cased */, null /* mode9 special-cased */
    };

    /// <summary>
    /// Decode a BC7Prep payload to raw BC7 blocks (16 bytes each). Returns null on any
    /// structural inconsistency (corrupt/unrecognized data).
    /// </summary>
    public static byte[]? Decode(FOodleTexture2DMipMap mip)
    {
        if (mip.Modes.Length != ModeCount) return mip.BulkData?.Data;

        var modes = mip.Modes;
        if (!ReadHeader(modes, out var numBlocks, out var payloadSize))
            return null;

        if (numBlocks == 0)
            return [];

        if (mip.BulkData is null || mip.BulkData.GetDataSize() < payloadSize || mip.BulkData.ReadDataOnce() is not { Length: >= 1 } payload)
        {
            Log.Warning("Bulk data is corrupted or missing");
            return null;
        }

        var decoded = new byte[numBlocks * 16];

        var modePos = new int[ModeCount + 1];
        for (var i = 0; i < ModeCount; i++)
            modePos[i + 1] = modePos[i] + modes[i] * ModeSizes[i];

        var modeNibbleOffset = modePos[ModeCount];

        var modeIndices = new List<int>[ModeCount];
        for (var i = 0; i < ModeCount; i++)
            modeIndices[i] = new List<int>(modes[i]);

        SortByMode(modeIndices, payload, modeNibbleOffset, numBlocks);

        var switchColorspace = (mip.OodleFlags & FlagSwitchColorspace) != 0;

        for (var mode = 0; mode < ModeCount; mode++)
        {
            var indices = modeIndices[mode];
            if (indices.Count == 0)
                continue;

            if (indices.Count != modes[mode])
                return null; // corrupt: nibble stream disagrees with header counts

            var splitPoint = SplitPoint[mode];
            var modeSize = ModeSizes[mode];
            var isSplit = (mip.OodleFlags & (FlagSplit0 << mode)) != 0;

            var firstOffset = modePos[mode];
            int stride0, stride1, secondOffset;
            if (isSplit)
            {
                stride0 = splitPoint;
                stride1 = modeSize - splitPoint;
                secondOffset = firstOffset + splitPoint * modes[mode];
            }
            else
            {
                stride0 = modeSize;
                stride1 = modeSize;
                secondOffset = firstOffset + splitPoint;
            }

            if (mode == 8)
                UnMungeMode8(decoded, payload, firstOffset, indices);
            else if (mode == 9)
                UnMungeMode9(decoded, payload, firstOffset, indices, switchColorspace);
            else
                Decoders[mode]!(decoded, payload, firstOffset, secondOffset, stride0, stride1, indices, switchColorspace);
        }

        return decoded;
    }

    private static bool ReadHeader(int[] modeCounts, out int numBlocks, out int payloadSize)
    {
        numBlocks = 0;
        payloadSize = 0;

        for (var i = 0; i < ModeCount; i++)
        {
            numBlocks += modeCounts[i];
            payloadSize += modeCounts[i] * ModeSizes[i];
        }

        payloadSize += (numBlocks + 1) / 2; // mode nibbles trailer
        return true;
    }

    private static void SortByMode(List<int>[] modeIndices, byte[] modeNibbles, int offset, int numBlocks)
    {
        for (var i = 0; i < numBlocks; i++)
        {
            var nibble = i % 2 == 0
                ? (byte)(modeNibbles[offset + i / 2] & 0xF)
                : (byte)((modeNibbles[offset + i / 2] >> 4) & 0xF);

            if (nibble < ModeCount)
                modeIndices[nibble].Add(i);
        }
    }

    // ---- Bit-manipulation helpers (ported from bc7prep_decode.cpp) ----

    private static ulong BitExtract(ulong val, int start, int width)
    {
        if (width == 0) return 0;
        var mask = (1UL << (width & 63)) - 1;
        return (val >> start) & mask;
    }

    private static ulong PackedAdd(ulong a, ulong b, ulong msbMask, ulong nonMsbMask)
    {
        var low = (a & nonMsbMask) + (b & nonMsbMask);
        return low ^ ((a ^ b) & msbMask);
    }

    private static ulong PackedAdd(ulong a, ulong b, ulong msbMask) => PackedAdd(a, b, msbMask, ~msbMask);

    private static void DecorrToRgbPacked(ref ulong r, ref ulong g, ref ulong b, ulong msbMask, ulong nonMsbMask)
    {
        var y = r; var cr = g; var cb = b;
        r = PackedAdd(y, cr, msbMask, nonMsbMask);
        g = y;
        b = PackedAdd(y, cb, msbMask, nonMsbMask);
    }

    private static void DecorrToRgbPacked(ref ulong r, ref ulong g, ref ulong b, ulong msbMask)
        => DecorrToRgbPacked(ref r, ref g, ref b, msbMask, ~msbMask);

    private static void DecorrToRgbScalar(ref int y, ref int cr, ref int cb, int mask)
    {
        var r = (y + cr) & mask;
        var g = y;
        var b = (y + cb) & mask;
        y = r; cr = g; cb = b;
    }

    private static ulong Mode4DecorrFast(ulong rgba)
    {
        var ybroad = ((uint)rgba & 0x3ffUL) * (1UL + (1UL << 10) + (1UL << 20));
        var crcba = ((rgba >> 10) & 0x3ff) | (rgba & 0xfffff00000UL);
        return PackedAdd(ybroad, crcba, 0x8421084210UL);
    }

    private static uint Compact32To7_2x(ulong x) => ((uint)x & 0x7f) | ((uint)(x >> 25) & 0x3f80);

    private static uint Compact24To7_3x(ulong x) =>
        ((uint)x & 0x7f) | ((uint)(x >> 17) & 0x3f80) | ((uint)(x >> 34) & 0x1fc000);

    private static uint Compact24To7_2x(ulong x) => ((uint)x & 0x7f) | ((uint)(x >> 17) & 0x3f80);

    private static uint Compact16To1_4x(ulong x)
    {
        x &= 0x0001000100010001UL;
        x *= 0x0001000200040008UL;
        return (uint)(x >> 48);
    }

    private static uint Compact16To1_2x(uint x) => (x & 1) | ((x >> 15) & 2);

    private static ulong Compact16To5_4x(ulong x)
    {
        x &= 0x001f001f001f001fUL;
        x = ((x >> 11) | x) & 0x000003ff000003ffUL;
        x = ((x >> 22) | x) & 0xfffff;
        return x;
    }

    private static uint Compact16To5_2x(uint x) => (x & 0x1f) | ((x >> 11) & 0x3e0);

    private static ulong Compact16To6_4x(ulong x)
    {
        x = ((x >> 10) | x) & 0x00000fff00000fffUL;
        x = ((x >> 20) | x) & 0xffffffUL;
        return x;
    }

    private static ulong Compact8To7_8x(ulong x)
    {
        const ulong stay1 = 0x007f007f007f007fUL;
        const ulong move1 = stay1 << 8;
        x = ((x & move1) >> 1) | (x & stay1);
        const ulong stay2 = 0x00003fff00003fffUL;
        x = ((x & ~stay2) >> 2) | (x & stay2);
        const ulong stay4 = 0x000000000fffffffUL;
        x = ((x & ~stay4) >> 4) | (x & stay4);
        return x;
    }

    private static ulong Compact8To7_6x(ulong x)
    {
        const ulong stay1 = 0x007f007f007fUL;
        const ulong move1 = stay1 << 8;
        x = ((x & move1) >> 1) | (x & stay1);
        x = ((x >> 0) & 0x0000000ffffUL)
          | ((x >> 2) & 0x0000fffc000UL)
          | ((x >> 4) & 0x3fff0000000UL);
        return x;
    }

    private static uint Compact8To1_8x(ulong x)
    {
        x &= 0x0101010101010101UL;
        x *= 0x0102040810204080UL;
        return (uint)(x >> 56);
    }

    private static ulong Expand4To5_12x(ulong x)
    {
        x = ((x & 0x0000ffff00000000UL) << 8)
          | ((x & 0x00000000ffff0000UL) << 4)
          | (x & 0x000000000000ffffUL);
        const ulong stay2 = 0x0000ff000ff000ffUL;
        x = ((x & ~stay2) << 2) | (x & stay2);
        x += x & 0x03c0f03c0f03c0f0UL;
        return x;
    }

    private static ulong Expand4To5_4x(ulong x)
    {
        x = ((x & 0x0ff00) << 2) | (x & 0x000ff);
        x += x & 0x3c0f0;
        return x;
    }

    private static uint Expand2To6_4x(uint x)
    {
        x = ((x << 8) | x) & 0x0f00f;
        x = ((x << 4) | x) & 0xc30c3;
        return x;
    }

    private static ulong Expand1To5_12x(ulong x)
    {
        x &= 0xfff;
        x = (x * 0x100010001UL) & 0xf0000f0000fUL;
        x = (x * 0x1111) & 0x084210842108421UL;
        return x;
    }

    private static ulong Expand1To5_4x(ulong x) => ((uint)(x & 0xf) * 0x1111u) & 0x8421;

    private static void WriteBlock(byte[] output, int idx, ulong lo, ulong hi)
    {
        var destOffset = idx * 16;
        BitConverter.TryWriteBytes(output.AsSpan(destOffset, 8), lo);
        BitConverter.TryWriteBytes(output.AsSpan(destOffset + 8, 8), hi);
    }

    private static ulong Get64(byte[] buf, int offset) => BitConverter.ToUInt64(buf, offset);
    private static uint Get32(byte[] buf, int offset) => BitConverter.ToUInt32(buf, offset);
    private static ulong Get16(byte[] buf, int offset) => BitConverter.ToUInt16(buf, offset);

    // ---- Per-mode decoders ----

    private static void UnMungeMode0(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var inLo = Get64(payload, firstOffset + i * stride0);
            var inHi = Get64(payload, secondOffset + i * stride1);

            var rbits = BitExtract(inLo, 0, 24);
            var gbits = BitExtract(inLo, 24, 24);
            var bbits = BitExtract(inLo, 48, 16) | (BitExtract(inHi, 0, 8) << 16);
            var partbits = BitExtract(inHi, 8, 4);

            if (switchColorspace)
                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x888888);

            var lo = 1UL;
            lo |= partbits << 1;
            lo |= rbits << 5;
            lo |= gbits << 29;
            lo |= bbits << 53;
            var hi = bbits >> 11;
            hi |= inHi & ~0x1fffUL;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode1(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var rgbs = Get64(payload, firstOffset + i * stride0);
            var extra = Get64(payload, secondOffset + i * stride1);

            var rbits = Compact16To6_4x((rgbs >> 0) & 0x003f003f003f003fUL);
            var gbits = Compact16To6_4x((rgbs >> 5) & 0x003e003e003e003eUL);
            var bbits = Compact16To6_4x((rgbs >> 10) & 0x003e003e003e003eUL);

            var expandedGb = Expand2To6_4x((uint)(extra & 0xff));
            gbits |= (expandedGb >> 0) & 0x41041;
            bbits |= (expandedGb >> 1) & 0x41041;

            if (switchColorspace)
                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x820820);

            var lo = 0x2UL;
            lo |= (extra >> 6) & 0xfc;
            lo |= rbits << 8;
            lo |= gbits << 32;
            lo |= bbits << 56;
            var hi = bbits >> 8;
            hi |= extra & ~0xffffUL;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode2(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var endpt0 = Get64(payload, firstOffset + i * stride0);
            var endpt1 = Get32(payload, firstOffset + i * stride0 + 8);
            var index = Get32(payload, secondOffset + i * stride1);

            var rbits = Compact16To5_4x(endpt0 >> 1) | ((ulong)Compact16To5_2x(endpt1 >> 1) << 20);
            var gbits = Compact16To5_4x(endpt0 >> 6) | ((ulong)Compact16To5_2x(endpt1 >> 6) << 20);
            var bbits = Compact16To5_4x(endpt0 >> 11) | ((ulong)Compact16To5_2x(endpt1 >> 11) << 20);
            var partbits = Compact16To1_4x(endpt0) | ((ulong)Compact16To1_2x(endpt1) << 4);

            if (switchColorspace)
                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x21084210);

            var lo = 0x4UL;
            lo |= partbits << 3;
            lo |= rbits << 9;
            lo |= gbits << 39;
            var hi = gbits >> 25;
            hi |= bbits << 5;
            hi |= (ulong)(index & ~7u) << 32;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode3(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var endpt0 = Get64(payload, firstOffset + i * stride0);
            var endpt1 = Get32(payload, firstOffset + i * stride0 + 8);
            var index = Get32(payload, secondOffset + i * stride1);

            var rbits = Compact24To7_3x(endpt0 >> 0) | (((ulong)endpt1 & 0x007f00) << (21 - 8));
            var gbits = Compact24To7_3x(endpt0 >> 8) | (((ulong)endpt1 & 0x7f0000) << (21 - 16));
            var bbits = Compact24To7_2x(endpt0 >> 16) | ((ulong)Compact24To7_2x(endpt1) << 14);
            var partbits = Compact8To1_8x(endpt0 >> 7);

            if (switchColorspace)
                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x8102040);

            var lo = 0x8UL;
            lo |= (partbits & 0x3f) << 4;
            lo |= rbits << 10;
            lo |= gbits << 38;
            var hi = gbits >> 26;
            hi |= bbits << 2;
            hi |= (ulong)(partbits & 0xc0) << 24;
            hi |= (ulong)index << 32;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode4(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var loPtr = firstOffset + i * stride0;
            var inLo = Get32(payload, loPtr) + (Get16(payload, loPtr + 4) << 32);
            var secondPtr = secondOffset + i * stride1;
            var inHi0 = Get16(payload, secondPtr);
            var inHi1 = Get64(payload, secondPtr + 2);

            var rgba = BitExtract(inLo, 2, 40);
            if (switchColorspace)
                rgba = Mode4DecorrFast(rgba);

            var crot = BitExtract(inHi0, 0, 2);
            var shiftAmount = (int)(((0UL - crot) & 3) * 10);

            var xorMask = (rgba ^ (rgba << shiftAmount)) & 0xffc0000000UL;
            rgba ^= xorMask;
            rgba ^= xorMask >> shiftAmount;

            rgba += rgba & 0x0ffc0000000UL;
            rgba += rgba & 0x1f000000000UL;

            rgba |= (((uint)(inLo & 3) * 0x21u) & 0x41u) * 1UL << 30;

            var lo = 0x10UL;
            lo |= crot << 5;
            lo |= BitExtract(inLo, 42, 1) << 7;
            lo |= rgba << 8;
            lo |= BitExtract(inHi0, 2, 14) << 50;
            var hi = inHi1;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode5(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var endpoints = Get64(payload, firstOffset + i * stride0);
            var indicesBits = Get64(payload, secondOffset + i * stride1);

            var lo = 0x20UL;
            ulong hi;

            if (switchColorspace)
            {
                var rbits = (endpoints >> 0) & 0x000000fe000000feUL;
                var gbits = (endpoints >> 8) & 0x000000fe000000feUL;
                var bbits = (endpoints >> 16) & 0x000000fe000000feUL;
                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x8000000080UL, 0x7e0000007eUL);
                endpoints = rbits | (gbits << 8) | (bbits << 16) | (endpoints & 0xff010101ff010101UL);

                var crot = (uint)indicesBits & 3;
                var shiftAmount = (int)(((0u - crot) & 3) << 3);

                var xorMask = (endpoints ^ (endpoints << shiftAmount)) & 0xff000000ff000000UL;
                endpoints ^= xorMask;
                endpoints ^= xorMask >> shiftAmount;

                lo |= (ulong)crot << 6;
                lo |= (ulong)Compact32To7_2x(endpoints >> 1) << 8;
                lo |= (ulong)Compact32To7_2x(endpoints >> 9) << 22;
                lo |= (ulong)Compact32To7_2x(endpoints >> 17) << 36;
                lo |= ((endpoints >> 24) & 0xff) << 50;
                lo |= ((endpoints >> 56) & 0x3f) << 58;
            }
            else
            {
                var crot = (uint)indicesBits & 3;
                var shiftAmount = (int)(((0u - crot) & 3) << 4);

                var xorMask = (endpoints ^ (endpoints << shiftAmount)) & 0xffff000000000000UL;
                endpoints ^= xorMask;
                endpoints ^= xorMask >> shiftAmount;

                lo |= (ulong)crot << 6;
                lo |= Compact8To7_6x(endpoints >> 1) << 8;
                lo |= ((endpoints >> 48) & 0xff) << 50;
                lo |= ((endpoints >> 56) & 0x3f) << 58;
            }

            hi = (endpoints >> 62) & 3;
            hi |= indicesBits & ~3UL;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode6(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var endpoints = Get64(payload, firstOffset + i * stride0);
            var indicesBits = Get64(payload, secondOffset + i * stride1);

            var lo = 0x40UL;

            if (switchColorspace)
            {
                var rbits = (ulong)Compact32To7_2x(endpoints >> 0);
                var gbits = (ulong)Compact32To7_2x(endpoints >> 8);
                var bbits = (ulong)Compact32To7_2x(endpoints >> 16);
                var abits = (ulong)Compact32To7_2x(endpoints >> 24);
                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x2040);

                lo |= rbits << 7;
                lo |= gbits << 21;
                lo |= bbits << 35;
                lo |= abits << 49;
            }
            else
            {
                var deint = Compact8To7_8x(endpoints);
                lo |= deint << 7;
            }

            lo |= endpoints & (1UL << 63);
            var hi = indicesBits;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode7(byte[] output, byte[] payload, int firstOffset, int secondOffset,
        int stride0, int stride1, List<int> indices, bool switchColorspace)
    {
        for (var i = 0; i < indices.Count; i++)
        {
            var prgbs = Get64(payload, firstOffset + i * stride0);
            var rest = (ulong)Get32(payload, firstOffset + i * stride0 + 8);
            rest |= (ulong)Get32(payload, secondOffset + i * stride1) << 32;

            ulong lo, hi;

            if (switchColorspace)
            {
                var rbits = Compact16To5_4x(prgbs >> 1);
                var gbits = Compact16To5_4x(prgbs >> 6);
                var bbits = Compact16To5_4x(prgbs >> 11);
                var pbits = Compact16To1_4x(prgbs);
                var abits = BitExtract(rest, 8, 20);

                DecorrToRgbPacked(ref rbits, ref gbits, ref bbits, 0x84210);

                lo = 0x80UL;
                lo |= BitExtract(rest, 28, 6) << 8;
                lo |= rbits << 14;
                lo |= gbits << 34;
                lo |= bbits << 54;
                hi = bbits >> 10;
                hi |= abits << 10;
                hi |= pbits << 30;
            }
            else
            {
                var pbits = BitExtract(rest, 8, 4);

                var rgbbits = Expand4To5_12x(prgbs) << 1;
                rgbbits |= Expand1To5_12x(rest >> 12);
                var abits = Expand4To5_4x(prgbs >> 48) << 1;
                abits |= Expand1To5_4x(rest >> 24);

                lo = 0x80UL;
                lo |= BitExtract(rest, 28, 6) << 8;
                lo |= rgbbits << 14;
                hi = rgbbits >> 50;
                hi |= abits << 10;
                hi |= pbits << 30;
            }

            hi |= rest & ~0x3FFFFffffUL;

            WriteBlock(output, indices[i], lo, hi);
        }
    }

    private static void UnMungeMode8(byte[] output, byte[] payload, int firstOffset, List<int> indices)
    {
        for (var i = 0; i < indices.Count; i++)
            Array.Copy(payload, firstOffset + i * 16, output, indices[i] * 16, 16);
    }

    private static void UnMungeMode9(byte[] output, byte[] payload, int firstOffset, List<int> indices, bool switchColorspace)
    {
        var t = 0;
        var coded = firstOffset;
        var count = indices.Count;

        while (t < count)
        {
            var r = payload[coded];
            var g = payload[coded + 1];
            var b = payload[coded + 2];

            if (switchColorspace)
            {
                int ri = r, gi = g, bi = b;
                DecorrToRgbScalar(ref ri, ref gi, ref bi, 255);
                r = (byte)ri; g = (byte)gi; b = (byte)bi;
            }

            const ulong bit6Mask = (0x40UL << 8) | (0x40UL << 22) | (0x40UL << 36);
            const ulong lo7Mask = (0x7fUL << 8) | (0x7fUL << 22) | (0x7fUL << 36);

            ulong colorBits = ((ulong)r << 8) | ((ulong)g << 22) | ((ulong)b << 36);

            var tBits = colorBits << 6;
            colorBits = ((colorBits >> 1) & lo7Mask) - (colorBits & ~lo7Mask);
            colorBits += tBits + (tBits & bit6Mask);

            colorBits |= (ulong)payload[coded + 3] << 50;
            colorBits |= 0x20;

            var codedBlock32 = Get32(payload, coded);

            do
            {
                WriteBlock(output, indices[t], colorBits, 0xaaaaaaacUL);
                t++;
                coded += 4;
                if (t >= count)
                    break;
            } while (Get32(payload, coded) == codedBlock32);
        }
    }
}
