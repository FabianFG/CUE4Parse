using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.Custom;

public partial class CustomFormatDecoder
{
    public static byte[] B4G4R4A4(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ * 2;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        var output = new byte[expectedSize * 2];
        B4G4R4A4(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    public static void B4G4R4A4(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var expectedSize = sizeX * sizeY * sizeZ * 2;
        var outputSize = expectedSize * 2;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        var inputSpan = input[..expectedSize].Cast<byte, ushort>();
        var outputSpan = output[..outputSize].Cast<byte, uint>();

        for (var i = 0; i < inputSpan.Length; i++)
        {
            var cl = inputSpan[i];
            outputSpan[i] = (uint)((cl & 0x000F) << 4 | (cl & 0x00F0) << 8 | (cl & 0x0F00) << 12 | (cl & 0xF000) << 16);
        }
    }
}
