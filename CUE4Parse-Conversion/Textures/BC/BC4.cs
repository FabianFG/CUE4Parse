using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
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
}
