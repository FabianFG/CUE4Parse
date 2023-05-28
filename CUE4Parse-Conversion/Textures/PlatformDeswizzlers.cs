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

    public static byte[] DeswizzlePS4(byte[] data, FTexture2DMipMap mip, FPixelFormatInfo formatInfo)
    {
        var outData = new byte[data.Length];
        var blockWidth = mip.SizeX / formatInfo.BlockSizeX;
        var blockHeight = mip.SizeY / formatInfo.BlockSizeY;
        var bpb = formatInfo.BlockBytes;

        var blockWidth2 = blockWidth > 8 ? blockWidth : 8;
        var blockHeight2 = blockHeight > 8 ? blockHeight : 8;

        for (var sy = 0; sy < blockHeight2; sy++)
        {
            for (var sx = 0; sx < blockWidth2; sx++)
            {
                var address = GetPS4TiledOffset(sx, sy, blockWidth2);
                var dy = address / blockWidth2;
                var dx = address % blockWidth2;
                if (dx >= blockWidth || dy >= blockHeight) continue;

                Buffer.BlockCopy(data, bpb * (sy * blockWidth2 + sx), outData, bpb * (dy * blockWidth + dx), bpb);
            }
        }

        return outData;
    }

    private static int GetPS4TiledOffset(int x, int y, int width)
    {
        (int mx, int my) = MapBlockPosition(x, y, width, 2);
        (mx, my) = MapBlockPosition(mx, my, width, 4);
        (mx, my) = MapBlockPosition(mx, my, width, 8);
        return mx + my * width;
    }

    private static (int xout, int yout) MapBlockPosition(int x, int y, int w, int bx)
    {
        var by = bx / 2;
        var ibx = x / bx;
        var iby = y / by;
        var obx = x % bx;
        var oby = y % by;
        var blockCountX = w / bx;
        var bl2S = 2 * blockCountX;
        var ll = ibx + iby * blockCountX;
        var ll2 = ll % bl2S;
        var ll22 = ll2 / 2 + ll2 % 2 * blockCountX;
        var llr = ll / bl2S * bl2S + ll22;
        var rbx = llr % blockCountX;
        var rby = llr / blockCountX;
        return (rbx * bx + obx, rby * by + oby);
    }
}
