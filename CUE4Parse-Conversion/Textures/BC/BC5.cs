using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public const int BC5BlockSize = 16;

    public static byte[] BC5(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, BC5BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC5(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void BC5(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, BC5BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC5(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void BC5(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, BC5BlockSize);

        var inputSpan = input[..inputLayerSize].Cast<byte, ulong>();
        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();

        Span<byte> r_bytes = stackalloc byte[16];
        Span<byte> g_bytes = stackalloc byte[16];
        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var index = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += 4)
            {
                DecodeBCBlock(inputSpan[index++], r_bytes);
                DecodeBCBlock(inputSpan[index++], g_bytes);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    var offset = xPixelOffset;
                    for (int row = 0; row < height; row++)
                    {
                        for (int col = 0; col < width; col++)
                        {
                            int i = (row << 2) + col;
                            outputSpan[offset + col] = (uint)(GetZNormal(r_bytes[i], g_bytes[i]) | g_bytes[i] << 8 | r_bytes[i] << 16 | 0xFF << 24);
                        }
                        offset += sizeX;
                    }
                }
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        int pixelOffset = xPixelOffset + (i >> 2) * sizeX + (i & 3);
                        outputSpan[pixelOffset] = (uint)(GetZNormal(r_bytes[i], g_bytes[i]) | g_bytes[i] << 8 | r_bytes[i] << 16 | 0xFF << 24);
                    }
                }
                xPixelOffset += 4;
            }
            yPixelOffset += 4 * sizeX;
        }
    }
}
