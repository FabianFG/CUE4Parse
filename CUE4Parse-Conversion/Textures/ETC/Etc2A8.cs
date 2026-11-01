using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse_Conversion.Textures.ETC;

public partial class EtcDecoder
{
    public const int Etc2A8BlockSize = 16;

    public static byte[] Etc2A8(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, Etc2A8BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            Etc2A8(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void Etc2A8(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, Etc2A8BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            Etc2A8(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void Etc2A8(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (_, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, Etc2A8BlockSize);

        var outputSpan = output[..outputLayerSize].Cast<byte, uint>();

        Span<uint> result = stackalloc uint[16];
        var resultSpan = result.Cast<uint, byte>();
        var row0 = result[..4];
        var row1 = result[4..8];
        var row2 = result[8..12];
        var row3 = result[12..];
        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var offset = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var xPixelOffset = yPixelOffset;
            for (int x = 0; x < sizeX; x += 4, offset += Etc2A8BlockSize)
            {
                var span = input.Slice(offset, Etc2A8BlockSize);
                DecodeEtc2Block(span[8..], result);
                DecodeEtc2A8Block(span, resultSpan);

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);

                    var dstoffset = xPixelOffset;
                    for (int row = 0; row < height; row++)
                    {
                        int srcOffset = row * 4;
                        for (int col = 0; col < width; col++)
                            outputSpan[dstoffset + col] = result[srcOffset + col];

                        dstoffset += sizeX;
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

    private static void DecodeEtc2A8Block(ReadOnlySpan<byte> input, Span<byte> output)
    {
        var data = BitConverter.ToUInt64(input);
        var baseCodeword = input[0] + 255;
        var i1 = input[1];
        var multiplier = i1 >> 4;
        output = output[..64];

        if (multiplier == 0)
        {
            var value = Clamp0_255[baseCodeword];
            output[ 3] = value;
            output[ 7] = value;
            output[11] = value;
            output[15] = value;
            output[19] = value;
            output[23] = value;
            output[27] = value;
            output[31] = value;
            output[35] = value;
            output[39] = value;
            output[43] = value;
            output[47] = value;
            output[51] = value;
            output[55] = value;
            output[59] = value;
            output[63] = value;
            return;
        }
        ulong selectors = BinaryPrimitives.ReverseEndianness(data) & 0xFFFFFFFFFFFF;
        var s1 = (int) selectors & 0xFFFFFF;
        var s2 = (int)(selectors >> 24) & 0xFFFFFF;
        var modifierTable = Etc2AModifierTable[i1 & 0x0F].AsSpan()[..8];
        ref byte clamp = ref MemoryMarshal.GetReference(Clamp0_255);
        output[ 3] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >> 21) & 7]);
        output[ 7] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >>  9) & 7]);
        output[11] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >> 21) & 7]);
        output[15] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >>  9) & 7]);
        output[19] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >> 18) & 7]);
        output[23] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >>  6) & 7]);
        output[27] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >> 18) & 7]);
        output[31] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >>  6) & 7]);
        output[35] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >> 15) & 7]);
        output[39] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >>  3) & 7]);
        output[43] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >> 15) & 7]);
        output[47] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >>  3) & 7]);
        output[51] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >> 12) & 7]);
        output[55] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s2 >>  0) & 7]);
        output[59] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >> 12) & 7]);
        output[63] = Unsafe.Add(ref clamp, baseCodeword + multiplier * modifierTable[(s1 >>  0) & 7]);
    }
}

