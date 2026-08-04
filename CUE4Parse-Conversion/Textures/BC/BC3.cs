using CommunityToolkit.HighPerformance;

namespace CUE4Parse_Conversion.Textures.BC;

public static partial class BCDecoder
{
    public static byte[] BC3(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var expectedSize = sizeX * sizeY * sizeZ;
        if (input.Length < expectedSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {expectedSize}");
        var output = new byte[expectedSize * 4];
        BC3(input, sizeX, sizeY, sizeZ, output);
        return output;
    }

    public static void BC3(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
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
        Span<byte> alpha = stackalloc byte[16];

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
                    var data = inputSpan[index++];
                    var data2 = inputSpan[index++];
                    ReadColorsBC3((uint)data2, colors);
                    uint bitmask = (uint)(data2 >> 32);
                    
                    var bits = (uint)(data >> 16);
                    var offset = xPixelOffset;
                    var cl = DecodeBCColors(data);
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

                    xPixelOffset += 4;
                }
                yPixelOffset += sizeX * 4;
            }
            zPixelOffset += sizeX * sizeY;
        }
    }
}
