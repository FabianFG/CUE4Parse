using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public const int BC1BlockSize = 8;

    public static byte[] BC1(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, BC1BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC1(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void BC1(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, BC1BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC1(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void BC1(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, BC1BlockSize);

        var inputSpan = input[..inputLayerSize].Cast<byte, ulong>();
        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();

        Span<uint> colors = stackalloc uint[4];
        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var index = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += 4)
            {
                var data = inputSpan[index++];
                ReadColorsBC1((uint)data, colors);
                uint bitmask = (uint)(data >> 32);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    var offset = xPixelOffset;
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    for (int row = 0; row < height; row++)
                    {
                        int shift = row * 8;
                        for (int col = 0; col < width; col++)
                        {
                            outputSpan[offset + col] = colors[(int)((bitmask >> (shift + col * 2)) & 3)];
                        }
                        offset += sizeX;
                    }
                }
                else
                {
                    var offset = xPixelOffset;
                    outputSpan[offset    ] = colors[(int)((bitmask >>  0) & 3)];
                    outputSpan[offset + 1] = colors[(int)((bitmask >>  2) & 3)];
                    outputSpan[offset + 2] = colors[(int)((bitmask >>  4) & 3)];
                    outputSpan[offset + 3] = colors[(int)((bitmask >>  6) & 3)];
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >>  8) & 3)];
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 10) & 3)];
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 12) & 3)];
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 14) & 3)];
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >> 16) & 3)];
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 18) & 3)];
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 20) & 3)];
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 22) & 3)];
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >> 24) & 3)];
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 26) & 3)];
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 28) & 3)];
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 30) & 3)];
                }
                xPixelOffset += 4;
            }
            yPixelOffset += sizeX * 4;
        }
    }
}
