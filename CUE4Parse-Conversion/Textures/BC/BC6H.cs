using System.Buffers.Binary;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    // only unsigned variant
    public static byte[] BC6H(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        var output = new byte[expectedSize * 4];
        BC6H(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    // only unsigned variant
    public static void BC6H(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        var outputSize = expectedSize * 4;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        var inputSpan = input[..expectedSize].Cast<byte, ulong>();
        var outputSpan = output[..outputSize].Cast<byte, uint>();

        Span<Half> halfs = stackalloc Half[16 * 4];
        var resultSpan = halfs.Cast<Half, ulong>();
        Span<float> floats = stackalloc float[16 * 4];
        Span<byte> bytes = stackalloc byte[16 * 4];
        var uints = bytes.Cast<byte, uint>();
        var row0 = uints[..4];
        var row1 = uints[4..8];
        var row2 = uints[8..12];
        var row3 = uints[12..];

        var index = 0;
        var zPixelLoc = 0;

        for (int z = 0; z < sizeZ; z++)
        {
            var yPixelLoc = zPixelLoc;
            for (int y = 0; y < sizeY / 4; y++)
            {
                var xPixelLoc = yPixelLoc;
                for (int x = 0; x < sizeX / 4; x++)
                {
                    DecodeBC6HBlock(inputSpan[index++], inputSpan[index++], resultSpan);
                    TensorPrimitives.ConvertToSingle(halfs, floats);
                    TensorPrimitives.Multiply(floats, 255.0f, floats);
                    TensorPrimitives.ConvertSaturating(floats, bytes);

                    row0.CopyTo(outputSpan.Slice(xPixelLoc, 4));
                    row1.CopyTo(outputSpan.Slice(xPixelLoc + sizeX, 4));
                    row2.CopyTo(outputSpan.Slice(xPixelLoc + 2 * sizeX, 4));
                    row3.CopyTo(outputSpan.Slice(xPixelLoc + 3 * sizeX, 4));

                    xPixelLoc += 4;
                }
                yPixelLoc += 4 * sizeX;
            }
            zPixelLoc += sizeX * sizeY;
        }
    }


    // http://graphics.stanford.edu/~seander/bithacks.html#VariableSignExtend
    public static uint ExtendSign(uint val, int bits)
    {
        return (uint)(((int)val << (32 - bits)) >> (32 - bits));
    }

    public static uint TransformInverse(uint val, uint a0, int bits)
    {
        // If the precision of A0 is "p" bits, then the transform algorithm is:
        // B0 = (B0 + A0) & ((1 << p) - 1)
        return (uint)(((int)val + (int)a0) & ((1 << bits) - 1));
    }

    /// <summary>
    /// Essentially copy-paste from documentation
    /// </summary>
    /// <param name="val"></param>
    /// <param name="bits"></param>
    /// <returns></returns>
    public static uint Unquantize(uint val, int bits)
    {
        if (bits >= 15)
        {
            return val;
        }
        else if (val == 0)
        {
            return 0;
        }
        else if (val == ((1 << bits) - 1))
        {
            return  0xFFFF;
        }
        else
        {
            return (uint)((((int)val << 16) + 0x8000) >> bits);
        }
    }

    public static uint Interpolate(uint a, uint b, int weight)
    {
        return (uint)(((int)a * (64 - weight) + (int)b * weight + 32) >> 6);
    }

    public static uint InterpolateNew(uint a, uint b, int weight)
    {
        return (uint)((((int)a << 6)+ ((int)b-(int)a) * weight + 32) >> 6);
    }

    public static ushort FinishUnquantize(uint val)
    {
        return (ushort)((((int)val << 5) - (int)val) >> 6); // scale the magnitude by 31 / 64
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint ReverseBits(this uint value)
    {
        value = ((value & 0xAAAAAAAA) >> 1) | ((value & 0x55555555) << 1);
        value = ((value & 0xCCCCCCCC) >> 2) | ((value & 0x33333333) << 2);
        value = ((value & 0xF0F0F0F0) >> 4) | ((value & 0x0F0F0F0F) << 4);

        return BinaryPrimitives.ReverseEndianness(value);
    }

    public static void DecodeBC6HBlock(ulong data1, ulong data2, Span<ulong> decompressedBlock)//, out int mode)
    {
        Span<uint> r = stackalloc uint[4]; // wxyz
        Span<uint> g = stackalloc uint[4];
        Span<uint> b = stackalloc uint[4];

        // uints variant for colors is slightly slower than ulong, but much cleaner
        var m = (uint)(data1 & 0x1F);
        var x = (uint)(data1 >> 5) & 0x3FFFFFFF;
        var y = (uint)(data1 >> 35) | (uint)(data2 & 1) << 29;
        var z = (uint)(data2 >> 1) & 0x1FFFF;

        uint r0 = x & 0x3FF;
        uint gy03 = (y >> 6) & 0x0F;
        uint g0 = (x >> 10) & 0x3FF;
        uint gz03 = (y >> 16) & 0x0F;
        uint b0 = (x >> 20) & 0x3FF;
        uint by03 = (y >> 26) & 0x0F;

        int mode;
        var modebits = (m & 0x2) == 0 ? m & 3 : m;
        switch (modebits)
        {
            case 0b00: // mode 1
                {
                    r[0] = r0;
                    r[1] = y & 0x1F;
                    r[2] = z & 0x1F;
                    r[3] = (z >> 6) & 0x1F;
                    g[0] = g0;
                    g[1] = (y >> 10) & 0x1F;
                    g[2] = (m & 0x04) << 2 | gy03;
                    g[3] = (y >> 1) & 0x10 | gz03;
                    b[0] = b0;
                    b[1] = (y >> 20) & 0x1F;
                    b[2] = (m & 0x8) << 1 | by03;
                    b[3] = (m & 0x10) | (y >> 15) & 0x01 | (y >> 24) & 0x02 | (z >> 3) & 0x04 | (z >> 8) & 0x08;
                    mode = 0;
                }
                break;
            case 0b01: // mode 2
                {
                    r[0] = x & 0x7F;
                    r[1] = y & 0x3F;
                    r[2] = z & 0x3F;
                    r[3] = (z >> 6) & 0x3F;
                    g[0] = (x >> 10) & 0x7F;
                    g[1] = (y >> 10) & 0x3F;
                    g[2] = (m & 0x04) << 3 | (x >> 15) & 0x10 | gy03;
                    g[3] = (m << 1) & 0x30 | gz03;
                    b[0] = (x >> 20) & 0x7F;
                    b[1] = (y >> 20) & 0x3F;
                    b[2] = (x >> 12) & 0x20 | (x >>  5) & 0x10 | (y >> 26) & 0x0F;
                    b[3] = (x >> 23) & 0x20 | (x >> 25) & 0x10 | (x >> 24) & 0x08 | (x >> 16) & 0x04 | (x >> 07) & 0x03;
                    mode = 1;
                }
                break;
            case 0b00010: // mode 3
                {
                    r[0] = r0 | (y << 5) & 0x400;
                    r[1] = y & 0x1F;
                    r[2] = z & 0x1F;
                    r[3] = (z >> 6) & 0x1F;
                    g[0] = g0 | (y >> 4) & 0x400;
                    g[1] = (y >> 10) & 0x0F;
                    g[2] = gy03;
                    g[3] = gz03;
                    b[0] = b0 | (y >> 14) & 0x400;
                    b[1] = (y >> 20) & 0x0F;
                    b[2] = by03;
                    b[3] = (y >> 15) & 0x01 | (y >> 24) & 0x02 | (z >> 3) & 0x04 | (z >> 8) & 0x08;
                    mode = 2;
                }
                break;
            case 0b00110: // mode 4
                {
                    r[0] = r0 | (y << 6) & 0x400;
                    r[1] = y & 0x0F;
                    r[2] = z & 0x0F;
                    r[3] = (z >> 6) & 0x0F;
                    g[0] = g0 | (y >> 5) & 0x400;
                    g[1] = (y >> 10) & 0x1F;
                    g[2] = gy03 | (z >> 6) & 0x10;
                    g[3] = (y >> 1) & 0x10 | gz03;
                    b[0] = b0 | (y >> 14) & 0x400;
                    b[1] = (y >> 20) & 0x0F;
                    b[2] = by03;
                    b[3] = (y >> 24) & 0x02 | (z >> 4) & 0x01 | (z >> 3) & 0x04 | (z >> 8) & 0x08;
                    mode = 3;
                }
                break;
            case 0b01010: // mode 5
                {
                    r[0] = r0 | (y << 6) & 0x400;
                    r[1] = y & 0x0F;
                    r[2] = z & 0x0F;
                    r[3] = (z >> 6) & 0x0F;
                    g[0] = g0 | (y >> 4) & 0x400;
                    g[1] = (y >> 10) & 0x0F;
                    g[2] = gy03;
                    g[3] = gz03;
                    b[0] = b0 | (y >> 15) & 0x400;
                    b[1] = (y >> 20) & 0x1F;
                    b[2] = (y >>  1) & 0x10 | by03;
                    b[3] = (y >> 15) & 0x01 | (z >> 3) & 0x06 | (z >> 6) & 0x10 | (z >> 8) & 0x08;
                    mode = 4;
                }
                break;
            case 0b01110: // mode 6
                {
                    r[0] = x & 0x1FF;
                    r[1] = y & 0x1F;
                    r[2] = z & 0x1F;
                    r[3] = (z >>  6) & 0x1F;
                    g[0] = (x >> 10) & 0x1FF;
                    g[1] = (y >> 10) & 0x1F;
                    g[2] = (x >> 15) & 0x10 | gy03;
                    g[3] = (y >>  1) & 0x10 | gz03;
                    b[0] = (x >> 20) & 0x1FF;
                    b[1] = (y >> 20) & 0x1F;
                    b[2] = (x >>  5) & 0x10 | (y >> 26) & 0x0F;
                    b[3] = (x >> 25) & 0x10 | (y >> 15) & 0x01 | (y >> 24) & 0x02 | (z >> 3) & 0x04 | (z >> 8) & 0x08;
                    mode = 5;
                }
                break;
            case 0b10010: // mode 7
                {
                    r[0] = x & 0xFF;
                    r[1] = y & 0x3F;
                    r[2] = z & 0x3F;
                    r[3] = (z >> 6) & 0x3F;
                    g[0] = (x >> 10) & 0xFF;
                    g[1] = (y >> 10) & 0x1F;
                    g[2] = (x >> 15) & 0x10 | (y >> 6) & 0x0F;
                    g[3] = (x >>  4) & 0x10 | gz03;
                    b[0] = (x >> 20) & 0xFF;
                    b[1] = (y >> 20) & 0x1F;
                    b[2] = (x >>  5) & 0x10 | by03;
                    b[3] = (x >> 16) & 0x04 | (x >> 25) & 0x18 | (y >> 15) & 0x01 | (y >> 24) & 0x02;
                    mode = 6;
                }
                break;
            case 0b10110: // mode 8
                {
                    r[0] = x & 0xFF;
                    r[1] = y & 0x1F;
                    r[2] = z & 0x1F;
                    r[3] = (z >> 6) & 0x1F;
                    g[0] = (x >> 10) & 0xFF;
                    g[1] = (y >> 10) & 0x3F;
                    g[2] = (x >> 13) & 0x20 | (x >> 15) & 0x10 | gy03;
                    g[3] = (x >> 23) & 0x20 | (y >>  1) & 0x10 | gz03;
                    b[0] = (x >> 20) & 0xFF;
                    b[1] = (y >> 20) & 0x1F;
                    b[2] = (x >>  5) & 0x10 | by03;
                    b[3] = (x >>  8) & 0x01 | (y >> 24) & 0x02 | (z >> 3) & 0x04 | (z >> 8) & 0x08 | (x >> 25) & 0x10;
                    mode = 7;
                }
                break;
            case 0b11010: // mode 9
                {
                    r[0] = x & 0xFF;
                    r[1] = y & 0x1F;
                    r[2] = z & 0x1F;
                    r[3] = (z >> 6) & 0x1F;
                    g[0] = (x >> 10) & 0xFF;
                    g[1] = (y >> 10) & 0x1F;
                    g[2] = (x >> 15) & 0x10 | gy03;
                    g[3] = (y >>  1) & 0x10 | gz03;
                    b[0] = (x >> 20) & 0xFF;
                    b[1] = (y >> 20) & 0x3F;
                    b[2] = (x >> 13) & 0x20 | (x >>  5) & 0x10 | by03;
                    b[3] = (x >>  7) & 0x02 | (x >> 23) & 0x20 | (x >> 25) & 0x10 | (y >> 15) & 0x01 | (z >> 3) & 0x04 | (z >> 8) & 0x08;
                    mode = 8;
                }
                break;
            case 0b11110: // mode 10
                {
                    r[0] = x & 0x3F;
                    r[1] = y & 0x3F;
                    r[2] = z & 0x3F;
                    r[3] = (z >> 6) & 0x3F;
                    g[0] = (x >> 10) & 0x3F;
                    g[1] = (y >> 10) & 0x3F;
                    g[2] = (x >> 11) & 0x20 | (x >> 15) & 0x10 | gy03;
                    g[3] = (x >>  2) & 0x10 | (x >> 21) & 0x20 | gz03;
                    b[0] = (x >> 20) & 0x3F;
                    b[1] = (y >> 20) & 0x3F;
                    b[2] = (x >>  5) & 0x10 | (x >> 12) & 0x20 | by03;
                    b[3] = (x >>  7) & 0x03 | (x >> 16) & 0x04 | (x >> 24) & 0x08 | (x >> 25) & 0x10 | (x >> 23) & 0x20;
                    mode = 9;
                }
                break;
            case 0b00011: // mode 11
                {
                    r[0] = r0;
                    r[1] = y & 0x3FF;
                    g[0] = g0;
                    g[1] = (y >> 10) & 0x3FF;
                    b[0] = b0;
                    b[1] = (y >> 20) & 0x3FF;
                    mode = 10;
                }
                break;
            case 0b00111: // mode 12
                {
                    r[0] = r0 | (y << 1) & 0x400;
                    r[1] = y & 0x1FF;
                    g[0] = g0 | (y >> 9) & 0x400;
                    g[1] = (y >> 10) & 0x1FF;
                    b[0] = b0 | (y >> 19) & 0x400;
                    b[1] = (y >> 20) & 0x1FF;
                    mode = 11;
                }
                break;
            case 0b01011: // mode 13
                {
                    r[0] = r0 | (y << 1) & 0x400 | (y << 3) & 0x800;
                    r[1] = y & 0xFF;
                    g[0] = g0 | (y >> 9) & 0x400| (y >> 7) & 0x800;
                    g[1] = (y >> 10) & 0xFF;
                    b[0] = b0 | (y >> 19) & 0x400 | (y >> 17) & 0x800;
                    b[1] = (y >> 20) & 0xFF;
                    mode = 12;
                }
                break;
            case 0b01111: // mode 14
                {
                    r[0] = r0 | ((y >> 4) & 0x3F).ReverseBits() >> 16;
                    r[1] = y & 0xF;
                    g[0] = g0 | ((y >> 14) & 0x3F).ReverseBits() >> 16;
                    g[1] = (y >> 10)  & 0xF;
                    b[0] = b0 | ((y >> 24) & 0x3F).ReverseBits() >> 16;
                    b[1] = (y >> 20) & 0xF;
                    mode = 13;
                }
                break;
            default:
                {
                    // Modes 10011, 10111, 11011, and 11111(not shown) are reserved.
                    // Do not use these in your encoder. If the hardware is passed blocks
                    // with one of these modes specified, the resulting decompressed block
                    // must contain all zeroes in all channels except for the alpha channel.
                    for (var i = 0; i < 16; i++)
                        decompressedBlock[i] = (ulong)0x3C << 56;
                    mode = 14;
                    return;
                }
        }

        int numPartitions = mode >= 10 ? 0 : 1;
        byte actualBits0Mode = BC6HTables.ActualBitsCount[0][mode];

        // Mode 11 (like Mode 10) does not use delta compression,
        // and instead stores both color endpoints explicitly.
        if (mode is not 9 and not 10)
        {
            for (int i = 1; i < (numPartitions + 1) * 2; ++i)
            {
                r[i] = TransformInverse(ExtendSign(r[i], BC6HTables.ActualBitsCount[1][mode]), r[0], actualBits0Mode);
                g[i] = TransformInverse(ExtendSign(g[i], BC6HTables.ActualBitsCount[2][mode]), g[0], actualBits0Mode);
                b[i] = TransformInverse(ExtendSign(b[i], BC6HTables.ActualBitsCount[3][mode]), b[0], actualBits0Mode);
            }
        }

        for (int i = 0; i < (numPartitions + 1) * 2; i++)
        {
            r[i] = Unquantize(r[i], actualBits0Mode);
            g[i] = Unquantize(g[i], actualBits0Mode);
            b[i] = Unquantize(b[i], actualBits0Mode);
        }

        int decompressedOffset = 0;
        var output = decompressedBlock[..16];
        if (mode >= 10)
        {
            var indexBits = data2 >> 1;
            ReadOnlySpan<int> weights = BC6HTables.AWeight4;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    int bitsCount = 4;
                    uint indexMask = 0x0F;
                    if ((i | j) == 0)
                    {
                        bitsCount = 3;
                        indexMask = 0x07;
                    }

                    int index = (int)(indexBits & indexMask);
                    indexBits >>= bitsCount;

                    var weight = weights[index];
                    ushort rFinal = FinishUnquantize(InterpolateNew(r[0], r[1], weight));
                    ushort gFinal = FinishUnquantize(InterpolateNew(g[0], g[1], weight));
                    ushort bFinal = FinishUnquantize(InterpolateNew(b[0], b[1], weight));

                    output[decompressedOffset++] = rFinal | (ulong)gFinal << 16 | (ulong)bFinal << 32 | (ulong)0x3C << 56;
                }
            }
        }
        else
        {
            uint partition = (z >> 12) & 0x1F;
            var indexBits = data2 >> 18;
            ReadOnlySpan<int> weights = BC6HTables.AWeight3;
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    int partitionSet = BC6HTables.PartitionSets[partition][i][j];
                    uint indexMask = 0x07;
                    int bitsCount = 3;
                    // fix-up index is specified with one less bit
                    // The fix-up index for subset 0 is always index 0
                    if ((partitionSet & 0x80) != 0)
                    {
                        bitsCount--;
                        indexMask = (1U << bitsCount) - 1;
                    }

                    partitionSet &= 0x01;
                    int index = (int)(indexBits & indexMask);
                    indexBits >>= bitsCount;

                    int ep_i = partitionSet * 2;
                    var weight = weights[index];
                    ushort rFinal = FinishUnquantize(InterpolateNew(r[ep_i], r[ep_i + 1], weight));
                    ushort gFinal = FinishUnquantize(InterpolateNew(g[ep_i], g[ep_i + 1], weight));
                    ushort bFinal = FinishUnquantize(InterpolateNew(b[ep_i], b[ep_i + 1], weight));

                    output[decompressedOffset++] = rFinal | (ulong)gFinal << 16 | (ulong)bFinal << 32 | (ulong)0x3C << 56;
                }
            }
        }
    }
}
