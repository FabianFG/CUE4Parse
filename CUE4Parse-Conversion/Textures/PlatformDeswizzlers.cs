using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.Textures;

public static class PlatformDeswizzlers
{
    static PlatformDeswizzlers()
    {
        PrepareDllFile();
    }

    [DllImport("tegra_swizzle_x64", EntryPoint = "deswizzle_block_linear")]
    private static extern unsafe void DeswizzleBlockLinearX64(ulong width, ulong height, ulong depth, byte* source, ulong sourceLength, byte[] destination, ulong destinationLength, ulong blockHeight, ulong bytesPerPixel);

    [DllImport("tegra_swizzle_x64", EntryPoint = "swizzled_surface_size")]
    private static extern ulong GetSurfaceSizeX64(ulong width, ulong height, ulong depth, ulong blockHeight, ulong bytesPerPixel);

    [DllImport("tegra_swizzle_x64", EntryPoint = "block_height_mip0")]
    private static extern ulong BlockHeightMip0X64(ulong height);

    [DllImport("tegra_swizzle_x64", EntryPoint = "mip_block_height")]
    private static extern ulong MipBlockHeightX64(ulong mipHeight, ulong blockHeightMip0);

    private static void PrepareDllFile()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CUE4Parse_Conversion.Resources.tegra_swizzle_x64.dll");
        if (stream == null)
            throw new MissingManifestResourceException("Couldn't find tegra_swizzle_x64.dll in Embedded Resources");
        var ba = new byte[(int) stream.Length];
        stream.Read(ba, 0, (int) stream.Length);

        bool fileOk;

        using (var sha1 = SHA1.Create())
        {
            var fileHash = BitConverter.ToString(sha1.ComputeHash(ba)).Replace("-", string.Empty);

            if (File.Exists("tegra_swizzle_x64.dll"))
            {
                var bb = File.ReadAllBytes("tegra_swizzle_x64.dll");
                var fileHash2 = BitConverter.ToString(sha1.ComputeHash(bb)).Replace("-", string.Empty);

                fileOk = fileHash == fileHash2;
            }
            else
            {
                fileOk = false;
            }
        }

        if (!fileOk)
        {
            File.WriteAllBytes("tegra_swizzle_x64.dll", ba);
        }
    }

    public static byte[] GetDeswizzledData(byte[] data, FTexture2DMipMap mip, FPixelFormatInfo formatInfo)
    {
        var heightInBlocks = formatInfo.GetBlockCountForHeight(mip.SizeY);
        // var blockHeightMip0 = BlockHeightMip0X64(heightInBlocks); // !!!!!! assuming heightInBlocks is ALWAYS using mip 0
        // var mipBlockHeightLog2 = Math.Log(MipBlockHeightX64(heightInBlocks, blockHeightMip0), 2);

        int blockHeight;
        switch (heightInBlocks)
        {
            case < 16:
                blockHeight = 1;
                break;
            case < 24:
                blockHeight = 2;
                break;
            case < 48:
                blockHeight = 4;
                break;
            default:
            {
                if (formatInfo is { BlockSizeX: 1, BlockSizeY: 1 } && heightInBlocks >= 96)
                    blockHeight = 16;
                else
                    blockHeight = 8;
                break;
            }
        }

        var widthInBlocks = formatInfo.GetBlockCountForWidth(mip.SizeX);
        var paddedWidth = mip.SizeX;
        // fine tune this
        if (mip.SizeY is > 128 and < 256)
        {
            widthInBlocks = (widthInBlocks + 127) & ~127;
            paddedWidth = widthInBlocks * formatInfo.BlockSizeX;
        }
        else if (mip.SizeY is > 256 and < 2048)
        {
            widthInBlocks = (widthInBlocks + 255) & ~255;
            paddedWidth = widthInBlocks * formatInfo.BlockSizeX;
        }

        var textureData = DeswizzleBlockLinear(paddedWidth, mip.SizeY, mip.SizeZ, formatInfo, blockHeight, data);
        if (paddedWidth <= mip.SizeX) return textureData;

        var rowSize = widthInBlocks * formatInfo.BlockBytes;
        var unpaddedRowSize = formatInfo.GetBlockCountForWidth(mip.SizeX) * formatInfo.BlockBytes;
        var unpaddedTextureData = new byte[heightInBlocks * unpaddedRowSize];
        for (int rowIndex = 0; rowIndex < heightInBlocks; rowIndex++)
        {
            Array.Copy(textureData, rowIndex * rowSize, unpaddedTextureData, rowIndex * unpaddedRowSize, unpaddedRowSize);
        }
        return unpaddedTextureData;
    }

    private static unsafe byte[] DeswizzleBlockLinear(int width, int height, int depth, FPixelFormatInfo formatInfo, int blockHeight, byte[] data)
    {
        width = formatInfo.GetBlockCountForWidth(width);
        height = formatInfo.GetBlockCountForHeight(height);
        depth = formatInfo.GetBlockCountForDepth(depth);
        var output = new byte[width * height * depth * formatInfo.BlockBytes];

        fixed (byte* ptr = data)
        {
            DeswizzleBlockLinearX64((ulong) width, (ulong) height, (ulong) depth, ptr, (ulong) data.Length, output, (ulong) output.Length, (ulong) blockHeight, (ulong) formatInfo.BlockBytes);
        }

        return output;
    }

    // https://github.com/tge-was-taken/GFD-Studio/blob/master/GFDLibrary/Textures/Swizzle/PS4SwizzleAlgorithm.cs
    public static byte[] DeswizzlePS4(byte[] data, FTexture2DMipMap mip, FPixelFormatInfo formatInfo)
    {
        var outData = new byte[data.Length];

        var heightTexels = mip.SizeY / formatInfo.BlockSizeX;
        var heightTexelsAligned = (heightTexels + 7) / 8;
        var widthTexels = mip.SizeX / formatInfo.BlockSizeY;
        var widthTexelsAligned = (widthTexels + 7) / 8;
        var dataIndex = 0;

        for (var y = 0; y < heightTexelsAligned; ++y)
        {
            for (var x = 0; x < widthTexelsAligned; ++x)
            {
                for (var t = 0; t < 64; ++t)
                {
                    var pixelIndex = Morton(t, 8, 8);
                    var num8 = pixelIndex / 8;
                    var num9 = pixelIndex % 8;
                    var yOffset = y * 8 + num8;
                    var xOffset = x * 8 + num9;

                    if (xOffset < widthTexels && yOffset < heightTexels)
                    {
                        var destPixelIdx = yOffset * widthTexels + xOffset;
                        var destIdx = formatInfo.BlockBytes * destPixelIdx;

                        Array.Copy(data, dataIndex, outData, destIdx, formatInfo.BlockBytes);
                    }

                    dataIndex += formatInfo.BlockBytes;
                }
            }
        }

        return outData;
    }

    private static int Morton(int t, int sx, int sy)
    {
        int num1;
        int num2 = num1 = 1;
        int num3 = t;
        int num4 = sx;
        int num5 = sy;
        int num6 = 0;
        int num7 = 0;

        while (num4 > 1 || num5 > 1)
        {
            if (num4 > 1)
            {
                num6 += num2 * (num3 & 1);
                num3 >>= 1;
                num2 *= 2;
                num4 >>= 1;
            }

            if (num5 > 1)
            {
                num7 += num1 * (num3 & 1);
                num3 >>= 1;
                num1 *= 2;
                num5 >>= 1;
            }
        }

        return num7 * sx + num6;
    }
}
