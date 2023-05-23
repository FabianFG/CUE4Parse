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

    public static byte[] GetDeswizzledData(byte[] data, FPixelFormatInfo formatInfo, int width, int height, int depth)
    {
        var blockHeightMip0 = BlockHeightMip0X64(formatInfo.GetBlockCountForHeight(height));
        var heightInBlocks = formatInfo.GetBlockCountForHeight(height);
        var mipBlockHeightLog2 = (int) Math.Log(MipBlockHeightX64(heightInBlocks, blockHeightMip0), 2);

        return DeswizzleBlockLinear(width, height, depth, formatInfo, mipBlockHeightLog2, data);
    }

    private static unsafe byte[] DeswizzleBlockLinear(int width, int height, int depth, FPixelFormatInfo formatInfo, int blockHeightLog2, byte[] data)
    {
        var x = formatInfo.GetBlockCountForWidth(width);
        var y = formatInfo.GetBlockCountForHeight(height);
        var z = formatInfo.GetBlockCountForDepth(depth);

        var blockHeight = 1 << Math.Max(Math.Min(blockHeightLog2, 5), 0);

        var output = new byte[width * height * formatInfo.BlockBytes];

        fixed (byte* ptr = data)
        {
            DeswizzleBlockLinearX64(x, y, z, ptr, (ulong) data.Length, output, (ulong) output.Length, (ulong) blockHeight, (ulong) formatInfo.BlockBytes);
        }

        return output;
    }

    public static byte[] DeswizzlePS4(byte[] data, int width, int height, int blockX, int blockY, int bpb)
    {
        var outData = new byte[data.Length];
        var blockWidth = width / blockX;
        var blockHeight = height / blockY;

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
