using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse_Conversion.Textures.ETC;

public partial class EtcDecoder
{
    public const int Etc1BlockSize = 8;

    public static byte[] Etc1(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, Etc1BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            Etc1(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void Etc1(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, Etc1BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            Etc1(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void Etc1(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (_, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, Etc1BlockSize);
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
            for (int x = 0; x < sizeX; x += 4, offset += Etc1BlockSize)
            {
                DecodeEtc1Block(input.Slice(offset, Etc1BlockSize), result);

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

    private static void DecodeEtc1Block(ReadOnlySpan<byte> input, Span<uint> output)
    {
        byte i3 = input[3];
        Span<int> c = stackalloc int[8];
        if ((i3 & 2) != 0)
        {
            var inp = input[0];
            var c1 = inp & 0xf8;
            var c2 = (byte)(c1 + ((inp << 29) >> 26));
            c[0] = c1 | c1 >> 5;
            c[4] = c2 | c2 >> 5;
            inp = input[1];
            c1 = inp & 0xf8;
            c2 = (byte)(c1 + ((inp << 29) >> 26));
            c[1] = c1 | c1 >> 5;
            c[5] = c2 | c2 >> 5;
            inp = input[2];
            c1 = inp & 0xf8;
            c2 = (byte)(c1 + ((inp << 29) >> 26));
            c[2] = c1 | c1 >> 5;
            c[6] = c2 | c2 >> 5;
        }
        else
        {
            var inp = input[0];
            c[0] = (inp & 0xf0 | inp >> 4);
            c[4] = (byte)(inp & 0x0f | inp << 4);
            inp = input[1];
            c[1] = (inp & 0xf0 | inp >> 4);
            c[5] = (byte)(inp & 0x0f | inp << 4);
            inp = input[2];
            c[2] = (inp & 0xf0 | inp >> 4);
            c[6] = (byte)(inp & 0x0f | inp << 4);
        }

        int j = input[7] | input[6] << 8;
        int k = input[5] << 1 | input[4] << 9;
        var code1 = (i3 >> 5) * 4;
        var code2 = (i3 & 28);
        var cl1 = c[..4];
        var cl2 = c[4..];
        if ((i3 & 1) == 0)
        {
            ProcessPixelETC1( 0, j, k, code1, cl1, output);
            ProcessPixelETC1( 1, j, k, code1, cl1, output);
            ProcessPixelETC1( 2, j, k, code1, cl1, output);
            ProcessPixelETC1( 3, j, k, code1, cl1, output);
            ProcessPixelETC1( 4, j, k, code1, cl1, output);
            ProcessPixelETC1( 5, j, k, code1, cl1, output);
            ProcessPixelETC1( 6, j, k, code1, cl1, output);
            ProcessPixelETC1( 7, j, k, code1, cl1, output);
            ProcessPixelETC1( 8, j, k, code2, cl2, output);
            ProcessPixelETC1( 9, j, k, code2, cl2, output);
            ProcessPixelETC1(10, j, k, code2, cl2, output);
            ProcessPixelETC1(11, j, k, code2, cl2, output);
            ProcessPixelETC1(12, j, k, code2, cl2, output);
            ProcessPixelETC1(13, j, k, code2, cl2, output);
            ProcessPixelETC1(14, j, k, code2, cl2, output);
            ProcessPixelETC1(15, j, k, code2, cl2, output);
        }
        else
        {
            ProcessPixelETC1( 0, j, k, code1, cl1, output);
            ProcessPixelETC1( 1, j, k, code1, cl1, output);
            ProcessPixelETC1( 2, j, k, code2, cl2, output);
            ProcessPixelETC1( 3, j, k, code2, cl2, output);
            ProcessPixelETC1( 4, j, k, code1, cl1, output);
            ProcessPixelETC1( 5, j, k, code1, cl1, output);
            ProcessPixelETC1( 6, j, k, code2, cl2, output);
            ProcessPixelETC1( 7, j, k, code2, cl2, output);
            ProcessPixelETC1( 8, j, k, code1, cl1, output);
            ProcessPixelETC1( 9, j, k, code1, cl1, output);
            ProcessPixelETC1(10, j, k, code2, cl2, output);
            ProcessPixelETC1(11, j, k, code2, cl2, output);
            ProcessPixelETC1(12, j, k, code1, cl1, output);
            ProcessPixelETC1(13, j, k, code1, cl1, output);
            ProcessPixelETC1(14, j, k, code2, cl2, output);
            ProcessPixelETC1(15, j, k, code2, cl2, output);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining| MethodImplOptions.AggressiveOptimization)]
        void ProcessPixelETC1(int i, int i1, int i2, int codeword, ReadOnlySpan<int> rgb, Span<uint> buffer)
        {
            var index = (codeword + ((i2 >> i) & 2 | (i1 >> i) & 1)) & 31;
            int modifier = Etc1ModifierTable[index] + 255;
            buffer[(i & 3) * 4 + ((i & 12) >> 2)] = (uint) (Clamp0_255[rgb[2] + modifier] << 16 |
                                                            Clamp0_255[rgb[1] + modifier] << 8 |
                                                            Clamp0_255[rgb[0] + modifier] | 0xFF << 24);
        }
    }
}

