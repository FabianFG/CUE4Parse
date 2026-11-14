using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public const int BC4BlockSize = 8;

    public static byte[] BC4(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, BC4BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC4(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void BC4(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, BC4BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            BC4(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void BC4(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, BC4BlockSize);

        var inputSpan = input[..inputLayerSize].Cast<byte, ulong>();
        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();

        Span<byte> bytes = stackalloc byte[16];
        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var index = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += 4)
            {
                DecodeBCBlock(inputSpan[index++], bytes);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    var offset = xPixelOffset;
                    for (int row = 0; row < height; row++)
                    {
                        for (int col = 0; col < width; col++)
                        {
                            byte gray = bytes[(row << 2) + col];
                            outputSpan[offset + col] = (uint)(gray | gray << 8 | gray << 16 | 0xFFu << 24);
                        }
                        offset += sizeX;
                    }
                }
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        byte gray = bytes[i];
                        int pixelLoc = xPixelOffset + (i >> 2) * sizeX + (i & 3);

                        outputSpan[pixelLoc] = (uint)(gray | gray << 8 | gray << 16 | 0xFFu << 24);
                    }
                }
                xPixelOffset += 4;
            }
            yPixelOffset += 4 * sizeX;
        }
    }
}
