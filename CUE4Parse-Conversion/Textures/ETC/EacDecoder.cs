// Portions derived from SwiftShader's ETC_Decoder.cpp https://swiftshader.googlesource.com/SwiftShader/+/HEAD/src/Device/ETC_Decoder.cpp
// Copyright 2016 The SwiftShader Authors. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse_Conversion.Textures.ETC;

internal static class EacDecoder
{
    private static readonly sbyte[,] _modifierTable =
    {
        { -3, -6, -9, -15, 2, 5, 8, 14 },
        { -3, -7, -10, -13, 2, 6, 9, 12 },
        { -2, -5, -8, -13, 1, 4, 7, 12 },
        { -2, -4, -6, -13, 1, 3, 5, 12 },
        { -3, -6, -8, -12, 2, 5, 7, 11 },
        { -3, -7, -9, -11, 2, 6, 8, 10 },
        { -4, -7, -8, -11, 3, 6, 7, 10 },
        { -3, -5, -8, -11, 2, 4, 7, 10 },
        { -2, -6, -8, -10, 1, 5, 7, 9 },
        { -2, -5, -8, -10, 1, 4, 7, 9 },
        { -2, -4, -8, -10, 1, 3, 7, 9 },
        { -2, -5, -7, -10, 1, 4, 6, 9 },
        { -3, -4, -7, -10, 2, 3, 6, 9 },
        { -1, -2, -3, -10, 0, 1, 2, 9 },
        { -4, -6, -8, -9, 3, 5, 7, 8 },
        { -3, -5, -7, -9, 2, 4, 6, 8 }
    };

    public static byte[] DecodeR11(byte[] source, int width, int height)
    {
        var blocksX = (width + 3) / 4;
        var blocksY = (height + 3) / 4;
        var expectedSize = checked(blocksX * blocksY * 8);
        if (source.Length < expectedSize)
            throw new ParserException($"EAC R11 texture data is too small (Expected: {expectedSize}, Actual: {source.Length})");

        var output = new byte[checked(width * height * 4)];
        var sourceOffset = 0;
        for (var blockY = 0; blockY < blocksY; blockY++)
        for (var blockX = 0; blockX < blocksX; blockX++, sourceOffset += 8)
        {
            var baseCodeword = source[sourceOffset];
            var multiplier = source[sourceOffset + 1] >> 4;
            var table = source[sourceOffset + 1] & 0x0F;
            ulong selectors = 0;
            for (var i = 2; i < 8; i++)
                selectors = (selectors << 8) | source[sourceOffset + i];

            for (var x = 0; x < 4; x++)
            for (var y = 0; y < 4; y++)
            {
                var pixelX = blockX * 4 + x;
                var pixelY = blockY * 4 + y;
                if (pixelX >= width || pixelY >= height)
                    continue;

                var selector = (int) (selectors >> (45 - 3 * (x * 4 + y))) & 7;
                var modifier = _modifierTable[table, selector];
                var value11 = multiplier == 0
                    ? baseCodeword * 8 + 4 + modifier
                    : baseCodeword * 8 + 4 + modifier * multiplier * 8;
                value11 = Math.Clamp(value11, 0, 2047);
                var value8 = (byte) ((value11 * 255 + 1023) / 2047);
                var destination = (pixelY * width + pixelX) * 4;
                output[destination] = value8;
                output[destination + 1] = value8;
                output[destination + 2] = value8;
                output[destination + 3] = byte.MaxValue;
            }
        }

        return output;
    }
}
