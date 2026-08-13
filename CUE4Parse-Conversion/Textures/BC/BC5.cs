using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
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
}
