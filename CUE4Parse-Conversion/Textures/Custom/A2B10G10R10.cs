using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.Custom;

public partial class CustomFormatDecoder
{
    public static byte[] A2B10G10R10(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ * 4;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        var output = new byte[expectedSize];
        A2B10G10R10(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    public static void A2B10G10R10(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var expectedSize = sizeX * sizeY * sizeZ * 4;
        var outputSize = expectedSize;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        var inputSpan = input[..expectedSize].Cast<byte, uint>();
        var outputSpan = output[..outputSize].Cast<byte, uint>();

        for (var i = 0; i < inputSpan.Length; i++)
        {
            var cl = inputSpan[i];
            outputSpan[i] = (cl >> 2) & 0xFF | (cl >> 4) & 0xFF00 | (cl >> 6) & 0xFF0000 | ((cl >> 30) * 85) << 24;
        }
    }
}
