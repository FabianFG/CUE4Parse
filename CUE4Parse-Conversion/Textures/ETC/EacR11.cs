using System.Buffers.Binary;
using System.Numerics.Tensors;
using CommunityToolkit.HighPerformance;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse_Conversion.Textures.ETC;

public partial class EtcDecoder
{
    public const int EacR11BlockSize = 8;

    public static byte[] EacR11(byte[] input, int sizeX, int sizeY, int sizeZ)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, out byte[] output, EacR11BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            EacR11(input.AsSpan(inputOffset, inputLayerSize), sizeX, sizeY, output.AsSpan(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }

        return output;
    }

    public static void EacR11(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output)
    {
        var (inputLayerSize, outputLayerSize) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, sizeZ, output, EacR11BlockSize);

        for (int i = 0, inputOffset = 0, outputOffset = 0; i < sizeZ; i++)
        {
            EacR11(input.Slice(inputOffset, inputLayerSize), sizeX, sizeY, output.Slice(outputOffset, outputLayerSize));
            inputOffset += inputLayerSize;
            outputOffset += outputLayerSize;
        }
    }

    public static void EacR11(ReadOnlySpan<byte> input, int sizeX, int sizeY, Span<byte> output)
    {
        var (inputLayerSize, _) = BCDecoder.ValidateAndGetLayerSizes(input, sizeX, sizeY, 1, output, EacR11BlockSize);

        var inputSpan = input[..inputLayerSize].Cast<byte, ulong>();

        var incompleteBlocks = (sizeX & 3) != 0 || (sizeY & 3) != 0;

        var index = 0;
        var yPixelOffset = 0;
        for (int y = 0; y < sizeY; y += 4)
        {
            var len = sizeX * Math.Min(4, sizeY - y) * 4;
            var outputSpan = output.Slice(yPixelOffset, len).Cast<byte, uint>();
            var outputIntSpan = outputSpan.Cast<uint, int>();

            var xPixelOffset = 0;
            for (int x = 0; x < sizeX; x += 4)
            {
                var selectors = inputSpan[index++];
                var baseCodeword = (byte)selectors * 8 + 4;
                var tablemult = (byte) (selectors >> 8);
                var multiplier = tablemult >> 4;
                multiplier = multiplier == 0 ? 1 : multiplier << 3;
                var modifierTable = Etc2AModifierTable[tablemult & 0x0F].AsSpan()[..8];
                selectors = BinaryPrimitives.ReverseEndianness(selectors) & 0xFFFFFFFFFFFF;

                if (incompleteBlocks && (x + 4 > sizeX || y + 4 > sizeY))
                {
                    var offset = xPixelOffset;
                    int width = Math.Min(4, sizeX - x);
                    int height = Math.Min(4, sizeY - y);
                    for (int row = 0; row < height; row++)
                    {
                        for (int col = 0; col < width; col++)
                        {
                            var selector = (int) (selectors >> (45 - 3 * (col * 4 + row))) & 7;
                            outputIntSpan[offset + col] = baseCodeword + multiplier * modifierTable[selector];
                        }
                        offset += sizeX;
                    }
                }
                else
                {
                    var offset = xPixelOffset;
                    outputIntSpan[offset    ] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 45) & 7];
                    outputIntSpan[offset + 1] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 33) & 7];
                    outputIntSpan[offset + 2] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 21) & 7];
                    outputIntSpan[offset + 3] = baseCodeword + multiplier * modifierTable[(int)(selectors >>  9) & 7];
                    offset += sizeX;
                    outputIntSpan[offset    ] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 42) & 7];
                    outputIntSpan[offset + 1] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 30) & 7];
                    outputIntSpan[offset + 2] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 18) & 7];
                    outputIntSpan[offset + 3] = baseCodeword + multiplier * modifierTable[(int)(selectors >>  6) & 7];
                    offset += sizeX;
                    outputIntSpan[offset    ] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 39) & 7];
                    outputIntSpan[offset + 1] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 27) & 7];
                    outputIntSpan[offset + 2] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 15) & 7];
                    outputIntSpan[offset + 3] = baseCodeword + multiplier * modifierTable[(int)(selectors >>  3) & 7];
                    offset += sizeX;
                    outputIntSpan[offset    ] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 36) & 7];
                    outputIntSpan[offset + 1] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 24) & 7];
                    outputIntSpan[offset + 2] = baseCodeword + multiplier * modifierTable[(int)(selectors >> 12) & 7];
                    outputIntSpan[offset + 3] = baseCodeword + multiplier * modifierTable[(int)(selectors >>  0) & 7];
                }
                xPixelOffset += 4;
            }

            TensorPrimitives.Clamp(outputIntSpan, 0 , 2047, outputIntSpan);
            TensorPrimitives.Multiply(outputSpan, 2041u, outputSpan);
            TensorPrimitives.Add(outputSpan, 8192u, outputSpan);
            TensorPrimitives.ShiftRightLogical(outputSpan, 14, outputSpan);
            TensorPrimitives.Xor(outputSpan, 0xFF000000, outputSpan);

            yPixelOffset += len;
        }
    }
}

