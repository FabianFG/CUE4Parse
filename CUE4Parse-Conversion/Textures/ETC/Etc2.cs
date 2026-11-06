using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse_Conversion.Textures.ETC;

public partial class EtcDecoder
{
    public const int Etc2BlockSize = 8;

    public static byte[] Etc2(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, Etc2BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            Etc2(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void Etc2(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, Etc2BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            Etc2(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void Etc2(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (_, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, Etc2BlockSize);
        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();

        Span<uint> result = stackalloc uint[16];
        var row0 = result[..4];
        var row1 = result[4..8];
        var row2 = result[8..12];
        var row3 = result[12..];
        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var offset = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += 4, offset += Etc2BlockSize)
            {
                DecodeEtc2Block(input.Slice(offset, Etc2BlockSize), result);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    var dstOffset = xPixelOffset;
                    for (int row = 0; row < height; row++)
                    {
                        int srcOffset = row * 4;
                        for (int col = 0; col < width; col++)
                            outputSpan[dstOffset + col] = result[srcOffset + col];

                        dstOffset += sizeX;
                    }
                }
                else
                {
                    row0.CopyTo(outputSpan.Slice(xPixelOffset, 4));
                    row1.CopyTo(outputSpan.Slice(xPixelOffset + sizeX, 4));
                    row2.CopyTo(outputSpan.Slice(xPixelOffset + 2 * sizeX, 4));
                    row3.CopyTo(outputSpan.Slice(xPixelOffset + 3 * sizeX, 4));
                }
                xPixelOffset += 4;
            }
            yPixelOffset += 4 * sizeX;
        }
    }

    private static void DecodeEtc2Block(ReadOnlySpan<byte> input, Span<uint> output)
    {
        if ((input[3] & 2) == 0)
        {
            DecodeEtc1Block(input, output);
            return;
        }

        int r = (input[0] & 0xf8) + ((input[0] << 29) >> 26);
        int g = (input[1] & 0xf8) + ((input[1] << 29) >> 26);
        int b = (input[2] & 0xf8) + ((input[2] << 29) >> 26);

        if ((r & 0xFF00) != 0)
        {
            ProcessEtc2BlockTMode(input, output);
        }
        else if ((g & 0xFF00) != 0)
        {
            ProcessEtc2BlockHMode(input, output);
        }
        else if ((b & 0xFF00) != 0)
        {
            ProcessEtc2BlockPlanarMode(input, output);
        }
        else
        {
            DecodeEtc1Block(input, output);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint ApplicateColor(ReadOnlySpan<int> rgb, int m)
    {
        m += 255;
        return (uint) (Clamp0_255[rgb[2] + m] << 16 | Clamp0_255[rgb[1] + m] << 8 | Clamp0_255[rgb[0] + m] | 0xFF << 24);
    }

    private static void ProcessEtc2BlockTMode(ReadOnlySpan<byte> input, Span<uint> output)
    {
        Span<int> c = stackalloc int[8];
        var inp = input[3];
        var ti = inp >> 1 & 6 | inp & 1;
        c[6] = inp & 0xf0 | inp >> 4;
        inp = input[0];
        c[0] = inp << 3 & 0xc0 | inp << 4 & 0x30 | inp >> 1 & 0xc | inp & 3;
        inp = input[1];
        c[1] = inp & 0xf0 | inp >> 4;
        c[2] = (byte)(inp & 0x0f | inp << 4);
        inp = input[2];
        c[4] = inp & 0xf0 | inp >> 4;
        c[5] = (byte)(inp & 0x0f | inp << 4);

        byte d = Etc2DistanceTable[ti];
        ReadOnlySpan<uint> colorSet =
        [
            (uint)(c[0] | c[1] << 8 | c[2] << 16 | 0xFFu << 24),
            ApplicateColor(c[4..], d),
            (uint)(c[4] | c[5] << 8 | c[6] << 16 | 0xFFu << 24),
            ApplicateColor(c[4..], -d)
        ];

        int j = input[7] | input[6] << 8;
        int k = input[5] << 1 | input[4] << 9;

        output[ 0] = colorSet[((k >>  0) & 2 | (j >>  0) & 1) & 3];
        output[ 4] = colorSet[((k >>  1) & 2 | (j >>  1) & 1) & 3];
        output[ 8] = colorSet[((k >>  2) & 2 | (j >>  2) & 1) & 3];
        output[12] = colorSet[((k >>  3) & 2 | (j >>  3) & 1) & 3];
        output[ 1] = colorSet[((k >>  4) & 2 | (j >>  4) & 1) & 3];
        output[ 5] = colorSet[((k >>  5) & 2 | (j >>  5) & 1) & 3];
        output[ 9] = colorSet[((k >>  6) & 2 | (j >>  6) & 1) & 3];
        output[13] = colorSet[((k >>  7) & 2 | (j >>  7) & 1) & 3];
        output[ 2] = colorSet[((k >>  8) & 2 | (j >>  8) & 1) & 3];
        output[ 6] = colorSet[((k >>  9) & 2 | (j >>  9) & 1) & 3];
        output[10] = colorSet[((k >> 10) & 2 | (j >> 10) & 1) & 3];
        output[14] = colorSet[((k >> 11) & 2 | (j >> 11) & 1) & 3];
        output[ 3] = colorSet[((k >> 12) & 2 | (j >> 12) & 1) & 3];
        output[ 7] = colorSet[((k >> 13) & 2 | (j >> 13) & 1) & 3];
        output[11] = colorSet[((k >> 14) & 2 | (j >> 14) & 1) & 3];
        output[15] = colorSet[((k >> 15) & 2 | (j >> 15) & 1) & 3];
    }

    private static void ProcessEtc2BlockHMode(ReadOnlySpan<byte> input, Span<uint> output)
    {
        Span<int> c = stackalloc int[8];
        var i3 = input[3];
        var i2 = input[2];
        var i1 = input[1];
        var i0 = input[0];
        c[6] = i3 << 1 & 0xf0 | i3 >> 3 & 0xf;
        c[0] = i0 << 1 & 0xf0 | i0 >> 3 & 0xf;
        c[1] = i0 << 5 & 0xe0 | i1 & 0x10;
        c[1] |= c[1] >> 4;
        c[2] = i1 & 8 | i1 << 1 & 6 | i2 >> 7;
        c[2] |= (byte)(c[2] << 4);
        c[4] = i2 << 1 & 0xf0 | i2 >> 3 & 0xf;
        c[5] = i2 << 5 & 0xe0 | i3 >> 3 & 0x10;
        c[5] |= c[5] >> 4;

        var bc1 = c[0] << 16 | c[1]  << 8 | c[2];
        var bc2 = c[4] << 16 | c[5]  << 8 | c[6];

        int di = i3 & 4 | i3 << 1 & 2 | (bc1 >= bc2 ? 1 : 0);
        byte d = Etc2DistanceTable[di];

        ReadOnlySpan<uint> colorSet =
        [
            ApplicateColor(c, d),
            ApplicateColor(c, -d),
            ApplicateColor(c[4..], d),
            ApplicateColor(c[4..], -d),
        ];

        int j = input[7] | input[6] << 8;
        int k = input[5] << 1 | input[4] << 9;

        output[ 0] = colorSet[((k >>  0) & 2 | (j >>  0) & 1) & 3];
        output[ 4] = colorSet[((k >>  1) & 2 | (j >>  1) & 1) & 3];
        output[ 8] = colorSet[((k >>  2) & 2 | (j >>  2) & 1) & 3];
        output[12] = colorSet[((k >>  3) & 2 | (j >>  3) & 1) & 3];
        output[ 1] = colorSet[((k >>  4) & 2 | (j >>  4) & 1) & 3];
        output[ 5] = colorSet[((k >>  5) & 2 | (j >>  5) & 1) & 3];
        output[ 9] = colorSet[((k >>  6) & 2 | (j >>  6) & 1) & 3];
        output[13] = colorSet[((k >>  7) & 2 | (j >>  7) & 1) & 3];
        output[ 2] = colorSet[((k >>  8) & 2 | (j >>  8) & 1) & 3];
        output[ 6] = colorSet[((k >>  9) & 2 | (j >>  9) & 1) & 3];
        output[10] = colorSet[((k >> 10) & 2 | (j >> 10) & 1) & 3];
        output[14] = colorSet[((k >> 11) & 2 | (j >> 11) & 1) & 3];
        output[ 3] = colorSet[((k >> 12) & 2 | (j >> 12) & 1) & 3];
        output[ 7] = colorSet[((k >> 13) & 2 | (j >> 13) & 1) & 3];
        output[11] = colorSet[((k >> 14) & 2 | (j >> 14) & 1) & 3];
        output[15] = colorSet[((k >> 15) & 2 | (j >> 15) & 1) & 3];
    }

    private static void ProcessEtc2BlockPlanarMode(ReadOnlySpan<byte> input, Span<uint> output)
    {
         Span<int> c = stackalloc int[12];
         var i3 = input[3];
         var i2 = input[2];
         var i1 = input[1];
         var i0 = input[0];
         c[0] = i0 << 1 & 0xfc | i0 >> 5 & 3;
         c[1] = i0 << 7 & 0x80 | i1 & 0x7e | i0 & 1;
         c[2] = i1 << 7 & 0x80 | i2 << 2 & 0x60 | i2 << 3 & 0x18 | i3 >> 5 & 4;
         c[2] |= c[2] >> 6;
         c[4] = (i3 << 1 & 0xf8 | i3 << 2 & 4 | i3 >> 5 & 3) - c[0];
         i3 = input[7];
         i2 = input[6];
         i1 = input[5];
         i0 = input[4];
         c[5] = (i0 & 0xfe | i0 >> 7) - c[1];
         c[6] = (i0 << 7 & 0x80 | i1 >> 1 & 0x7c);
         c[6] = (c[6] | c[6] >> 6) - c[2];
         c[8] = (i1 << 5 & 0xe0 | i2 >> 3 & 0x1c | i1 >> 1 & 3) - c[0];
         c[9] = (byte) (i2 << 3 & 0xf8 | i3 >> 5 & 0x6 | i2 >> 4 & 1) - c[1];
         c[10] = (byte) (i3 << 2 | i3 >> 4 & 3) - c[2];
         c[0] = 4 * c[0] + 2;
         c[1] = 4 * c[1] + 2;
         c[2] = 4 * c[2] + 2;

         output = output[..16];
         for (int y = 0; y < 4; y++)
         {
             for (int x = 0; x < 4; x++)
             {
                 int r = Clamp0_255[((x * c[4] + y * c[8] + c[0]) >> 2) + 255];
                 int g = Clamp0_255[((x * c[5] + y * c[9] + c[1]) >> 2) + 255];
                 int b = Clamp0_255[((x * c[6] + y * c[10] + c[2]) >> 2) + 255];
                 output[y * 4 + x] = (uint) (r | g << 8 | b << 16 | 0xFF << 24);
             }
         }
    }
}
