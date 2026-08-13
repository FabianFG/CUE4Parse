using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public static byte[] BC2(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        var output = new byte[expectedSize * 4];
        BC2(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    public static void BC2(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        var outputSize = expectedSize * 4;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        var inputSpan = input[..expectedSize].Cast<byte, ulong>();
        var outputSpan = output[..outputSize].Cast<byte, uint>();
        Span<uint> colors = stackalloc uint[4];

        var index = 0;
        var zPixelOffset = 0;
        for (int z = 0; z < sizeZ; z++)
        {
            var yPixelOffset = zPixelOffset;
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

                    xPixelOffset += 4;
                }
                yPixelOffset += sizeX * 4;
            }
            zPixelOffset += sizeX * sizeY;
        }
    }
}
