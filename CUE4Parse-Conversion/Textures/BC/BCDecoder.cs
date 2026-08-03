using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static class BCDecoder
{
    public static byte[] BC4(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ / 2;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        var output = new byte[expectedSize * 8];
        BC4(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    public static void BC4(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var expectedSize = sizeX * sizeY * sizeZ / 2;
        var outputSize = expectedSize * 8;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        var inputSpan = input[..expectedSize].Cast<byte, ulong>();
        var outputSpan = output[..outputSize].Cast<byte, uint>();
        Span<byte> bytes = stackalloc byte[16];

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
                    DecodeBCBlock(inputSpan[index++], bytes);

                    for (int i = 0; i < 16; i++)
                    {
                        byte gray = bytes[i];
                        int pixelLoc = xPixelLoc + (i >> 2) * sizeX + (i & 3);

                        outputSpan[pixelLoc] = (uint)(gray | gray << 8 | gray << 16 | 0xFFu << 24);
                    }
                    xPixelLoc += 4;
                }
                yPixelLoc += 4 * sizeX;
            }
            zPixelLoc += sizeX * sizeY;
        }
    }

    public static byte[] BC5(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} does not match expected size {expectedSize}");
        var output = new byte[expectedSize * 4];
        BC5(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    public static void BC5(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        var outputSize = expectedSize * 4;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} does not match expected size {expectedSize}");
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        var inputSpan = input[..expectedSize].Cast<byte, ulong>();
        var outputSpan = output[..outputSize].Cast<byte, uint>();
        Span<byte> r_bytes = stackalloc byte[16];
        Span<byte> g_bytes = stackalloc byte[16];

        var index = 0;
        var zPixelLoc = 0;

        for (var z = 0; z < sizeZ; z++)
        {
            var yPixelLoc = zPixelLoc;
            for (int y = 0; y < sizeY / 4; y++)
            {
                var xPixelLoc = yPixelLoc;
                for (int x = 0; x < sizeX / 4; x++)
                {
                    DecodeBCBlock(inputSpan[index++], r_bytes);
                    DecodeBCBlock(inputSpan[index++], g_bytes);

                    for (int i = 0; i < 16; i++)
                    {
                        int pixelLoc = xPixelLoc + (i >> 2) * sizeX + (i & 3);
                        outputSpan[pixelLoc] = (uint)(GetZNormal(r_bytes[i], g_bytes[i]) | g_bytes[i] << 8 | r_bytes[i] << 16 | 0xFF << 24); 
                    }
                    xPixelLoc += 4;
                }
                yPixelLoc += 4 * sizeX;
            }
            zPixelLoc += sizeX * sizeY;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetZNormal(byte x, byte y)
    {
        const float scale = 2.0f / 255.0f;
        var xf = x * scale - 1;
        var yf = y * scale - 1;
        var zval = 1 - xf * xf - yf * yf;
        var zval_ = (float)MathF.Sqrt(zval > 0 ? zval : 0);
        zval = zval_ < 1.0f ? zval_ : 1.0f;
        return (byte)((zval * 127) + 128);
    }

    private static void DecodeBCBlock(ulong data, Span<byte> block)
    {
        Span<byte> cl = stackalloc byte[8];
        cl[0] = (byte) data;
        cl[1] = (byte)(data >> 8);

        if (cl[0] > cl[1])
        {
            var diff = cl[0] - cl[1];
            var temp = 6 * cl[0] + cl[1];
            cl[2] = (byte)(temp / 7);
            temp -= diff;
            cl[3] = (byte)(temp / 7);
            temp -= diff;
            cl[4] = (byte)(temp / 7);
            temp -= diff;
            cl[5] = (byte)(temp / 7);
            temp -= diff;
            cl[6] = (byte)(temp / 7);
            temp -= diff;
            cl[7] = (byte)(temp / 7);
        }
        else
        {
            var diff = cl[1] - cl[0];
            var temp = 4 * cl[0] + cl[1];
            cl[2] = (byte)(temp / 5);
            temp += diff;
            cl[3] = (byte)(temp / 5);
            temp += diff;
            cl[4] = (byte)(temp / 5);
            temp += diff;
            cl[5] = (byte)(temp / 5);
            cl[6] = 0;
            cl[7] = 255;
        }

        var bits = (uint)(data >> 16);
        block[0] = cl[(int)((bits >>  0) & 7)];
        block[1] = cl[(int)((bits >>  3) & 7)];
        block[2] = cl[(int)((bits >>  6) & 7)];
        block[3] = cl[(int)((bits >>  9) & 7)];
        block[4] = cl[(int)((bits >> 12) & 7)];
        block[5] = cl[(int)((bits >> 15) & 7)];
        block[6] = cl[(int)((bits >> 18) & 7)];
        block[7] = cl[(int)((bits >> 21) & 7)];
        bits = (uint)(data >> 40);
        block[8]  = cl[(int)((bits >>  0) & 7)];
        block[9]  = cl[(int)((bits >>  3) & 7)];
        block[10] = cl[(int)((bits >>  6) & 7)];
        block[11] = cl[(int)((bits >>  9) & 7)];
        block[12] = cl[(int)((bits >> 12) & 7)];
        block[13] = cl[(int)((bits >> 15) & 7)];
        block[14] = cl[(int)((bits >> 18) & 7)];
        block[15] = cl[(int)((bits >> 21) & 7)];
    }
}