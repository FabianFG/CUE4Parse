using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public const int BC2BlockSize = 16;

    public static byte[] BC2(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, BC2BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC2(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void BC2(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, BC2BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC2(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void BC2(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, BC2BlockSize);

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
                ulong alphaBits = inputSpan[index++];
                var data = inputSpan[index++];
                ReadColorsBC3((uint)data, colors);
                uint bitmask = (uint)(data >> 32);

                var offset = xPixelOffset;
                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {

                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    for (int row = 0; row < height; row++)
                    {
                        for (int col = 0; col < width; col++)
                        {
                            var shift = row * 8 + col * 2;
                            outputSpan[offset + col] = colors[(int)((bitmask >> shift) & 3)] | (((uint)(alphaBits >> (shift * 2) & 0xF) * 17) << 24);
                        }
                        offset += sizeX;
                    }
                }
                else
                {
                    var bits = (uint)alphaBits;
                    outputSpan[offset    ] = colors[(int)((bitmask >>  0) & 3)] | ((((bits >>  0) & 0xF) * 17) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >>  2) & 3)] | ((((bits >>  4) & 0xF) * 17) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >>  4) & 3)] | ((((bits >>  8) & 0xF) * 17) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >>  6) & 3)] | ((((bits >> 12) & 0xF) * 17) << 24);
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >>  8) & 3)] | ((((bits >> 16) & 0xF) * 17) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 10) & 3)] | ((((bits >> 20) & 0xF) * 17) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 12) & 3)] | ((((bits >> 24) & 0xF) * 17) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 14) & 3)] | ((((bits >> 28) & 0xF) * 17) << 24);
                    bits = (uint)(alphaBits >> 32);
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >> 16) & 3)] | ((((bits >>  0) & 0xF) * 17) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 18) & 3)] | ((((bits >>  4) & 0xF) * 17) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 20) & 3)] | ((((bits >>  8) & 0xF) * 17) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 22) & 3)] | ((((bits >> 12) & 0xF) * 17) << 24);
                    offset += sizeX;
                    outputSpan[offset    ] = colors[(int)((bitmask >> 24) & 3)] | ((((bits >> 16) & 0xF) * 17) << 24);
                    outputSpan[offset + 1] = colors[(int)((bitmask >> 26) & 3)] | ((((bits >> 20) & 0xF) * 17) << 24);
                    outputSpan[offset + 2] = colors[(int)((bitmask >> 28) & 3)] | ((((bits >> 24) & 0xF) * 17) << 24);
                    outputSpan[offset + 3] = colors[(int)((bitmask >> 30) & 3)] | ((((bits >> 28) & 0xF) * 17) << 24);
                }
                xPixelOffset += 4;
            }
            yPixelOffset += sizeX * 4;
        }
    }
}
