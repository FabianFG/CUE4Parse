using System.Buffers.Binary;
using CommunityToolkit.HighPerformance;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse_Conversion.Textures.ETC;

public partial class EtcDecoder
{
    public const int EacRG11BlockSize = 16;

    public static byte[] EacRG11(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, EacRG11BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            EacRG11(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void EacRG11(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, EacRG11BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            EacRG11(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void EacRG11(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, EacRG11BlockSize);
        var inputSpan = input[..inputLayerSize].Cast<byte, ulong>();
        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();

        Span<uint> result = stackalloc uint[16];
        var row0 = result[..4];
        var row1 = result[4..8];
        var row2 = result[8..12];
        var row3 = result[12..];
        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var index = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += 4)
            {
                DecodeEacRG11Block(inputSpan[index++],inputSpan[index++], result);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    var dstOffset = xPixelOffset;
                    for (int row = 0; row < height; row++)
                    {
                        int srcOffset = row * 4;
                        for (int col = 0; col < width; col++)
                            outputSpan[dstOffset + col] = result[srcOffset + col];

                        dstOffset += sizeX;
                    }
                }
                else
                {
                    row0.CopyTo(outputSpan.Slice(xPixelOffset, 4));
                    row1.CopyTo(outputSpan.Slice(xPixelOffset + sizeX, 4));
                    row2.CopyTo(outputSpan.Slice(xPixelOffset + 2 * sizeX, 4));
                    row3.CopyTo(outputSpan.Slice(xPixelOffset + 3 * sizeX, 4));
                }
                xPixelOffset += 4;
            }
            yPixelOffset += 4 * sizeX;
        }
    }

    private static void DecodeEacRG11Block(ulong rinput, ulong ginput, Span<uint> output)
    {
        var rBase = (byte)rinput * 8 + 4;
        var rtablemult = (byte) (rinput >> 8);
        var rMult = rtablemult >> 4;
        rMult = rMult == 0 ? 1 : rMult << 3;
        var rTable = Etc2AModifierTable[rtablemult & 0x0F].AsSpan()[..8];
        var rInd = BinaryPrimitives.ReverseEndianness(rinput) & 0xFFFFFFFFFFFF;

        var gBase = (byte)ginput * 8 + 4;
        var gtablemult = (byte) (ginput >> 8);
        var gMult = gtablemult >> 4;
        gMult = gMult == 0 ? 1 : gMult << 3;
        var gTable = Etc2AModifierTable[gtablemult & 0x0F].AsSpan()[..8];
        var gInd = BinaryPrimitives.ReverseEndianness(ginput) & 0xFFFFFFFFFFFF;

        output = output[..16];
        output[ 0] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 45) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 45) & 7]) << 8 | 0xFF <<24);
        output[ 1] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 33) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 33) & 7]) << 8 | 0xFF <<24);
        output[ 2] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 21) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 21) & 7]) << 8 | 0xFF <<24);
        output[ 3] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >>  9) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >>  9) & 7]) << 8 | 0xFF <<24);
        output[ 4] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 42) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 42) & 7]) << 8 | 0xFF <<24);
        output[ 5] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 30) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 30) & 7]) << 8 | 0xFF <<24);
        output[ 6] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 18) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 18) & 7]) << 8 | 0xFF <<24);
        output[ 7] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >>  6) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >>  6) & 7]) << 8 | 0xFF <<24);
        output[ 8] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 39) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 39) & 7]) << 8 | 0xFF <<24);
        output[ 9] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 27) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 27) & 7]) << 8 | 0xFF <<24);
        output[10] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 15) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 15) & 7]) << 8 | 0xFF <<24);
        output[11] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >>  3) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >>  3) & 7]) << 8 | 0xFF <<24);
        output[12] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 36) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 36) & 7]) << 8 | 0xFF <<24);
        output[13] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 24) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 24) & 7]) << 8 | 0xFF <<24);
        output[14] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >> 12) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >> 12) & 7]) << 8 | 0xFF <<24);
        output[15] = (uint)(Clamp2047ToByte(rBase + rMult * rTable[(int)(rInd >>  0) & 7]) | Clamp2047ToByte(gBase + gMult * gTable[(int)(gInd >>  0) & 7]) << 8 | 0xFF <<24);

        int Clamp2047ToByte(int v) => (Math.Clamp(v, 0, 2047) * 2041 + 8192) >> 14;
    }
}

