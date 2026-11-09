using System.Runtime.CompilerServices;

namespace CUE4Parse_Conversion.Textures.ASTC;

public static partial class ASTCDecoder
{
    private static void ValidateBlockDimension(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 4 || value > 12)
        {
            throw new ArgumentException($"Block dimension must be between 4 and 12, inclusive. Value: {value}", paramName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int InputLayerSize, int OutputLayerSize) ValidateAndGetLayerSizes(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, out byte[] output, int blockSizeX, int blockSizeY)
    {
        ValidateBlockDimension(blockSizeX);
        ValidateBlockDimension(blockSizeY);
        var inputLayerSize = AstcLayerSize(sizeX, sizeY, blockSizeX, blockSizeY);
        var inputSize = inputLayerSize * sizeZ;
        if (input.Length < inputSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {inputSize}");

        var outputLayerSize = sizeX * sizeY * sizeof(uint);
        var outputSize = outputLayerSize * sizeZ;
        output = new byte[outputSize];

        return (inputLayerSize, outputLayerSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int InputLayerSize, int OutputLayerSize) ValidateAndGetLayerSizes(ReadOnlySpan<byte> input, int sizeX, int sizeY, int sizeZ, Span<byte> output, int blockSizeX, int blockSizeY)
    {
        ValidateBlockDimension(blockSizeX);
        ValidateBlockDimension(blockSizeY);
        var inputLayerSize = AstcLayerSize(sizeX, sizeY, blockSizeX, blockSizeY);
        var inputSize = inputLayerSize * sizeZ;
        if (input.Length < inputSize)
            throw new ArgumentException($"Input length {input.Length} is smaller than expected size {inputSize}");

        var outputLayerSize = sizeX * sizeY * sizeof(uint);
        var outputSize = outputLayerSize * sizeZ;
        if (output.Length < outputSize)
            throw new ArgumentException($"Output length {output.Length} is smaller than expected size {outputSize}");

        return (inputLayerSize, outputLayerSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int AstcLayerSize(int sizeX, int sizeY, int blockSizeX, int blockSizeY)
    {
        var blocksX = (sizeX + blockSizeX - 1) / blockSizeX;
        var blocksY = (sizeY + blockSizeY - 1) / blockSizeY;
        return blocksX * blocksY * AstcBlockSize;
    }
}
