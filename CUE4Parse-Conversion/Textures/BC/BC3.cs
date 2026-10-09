using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public const int BC3BlockSize = 16;

    public static byte[] BC3(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, BC3BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC3(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void BC3(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, BC3BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC3(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void BC3(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, BC3BlockSize);

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
                var data2 = inputSpan[index++];
                ReadColorsBC3((uint)data2, colors);
                uint bitmask = (uint)(data2 >> 32);
                var cl = DecodeBCColors(data);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    var offset = xPixelOffset;
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    var bits = data >> 16;
                    for (int row = 0; row < height; row++)
                    {
                        for (int col = 0; col < width; col++)
                        {
                            var shift = row * 4 + col;
                            var colorIndex = (int) ((bitmask >> (shift * 2)) & 3);
                            var alphaIndex = (int) ((bits >> (shift * 3)) & 7);
                            outputSpan[offset + col] = colors[colorIndex] | ((uint) (byte) (cl >> (alphaIndex << 3)) << 24);
                        }
                        offset += sizeX;
                    }
                }
                else
                {
                    var bits = (uint)(data >> 16);
                    var offset = xPixelOffset;
                    outputSpan[offset    ] = colors[(int)((bitmask >>  0) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  0) & 7) << 3)) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >>  2) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  3) & 7) << 3)) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >>  4) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  6) & 7) << 3)) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >>  6) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  9) & 7) << 3)) << 24);
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >>  8) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 12) & 7) << 3)) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 10) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 15) & 7) << 3)) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 12) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 18) & 7) << 3)) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 14) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 21) & 7) << 3)) << 24);

                    bits = (uint) (data >> 40);
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >> 16) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  0) & 7) << 3)) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 18) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  3) & 7) << 3)) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 20) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  6) & 7) << 3)) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 22) & 3)] | (uint)((byte)(cl >> (int)(((bits >>  9) & 7) << 3)) << 24);
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >> 24) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 12) & 7) << 3)) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 26) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 15) & 7) << 3)) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 28) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 18) & 7) << 3)) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 30) & 3)] | (uint)((byte)(cl >> (int)(((bits >> 21) & 7) << 3)) << 24);
                }
                xPixelOffset += 4;
            }
            yPixelOffset += sizeX * 4;
        }
    }
}
