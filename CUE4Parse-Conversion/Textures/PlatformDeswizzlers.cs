using System;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse_Conversion.Textures;

public static class PlatformDeswizzlers
{
    // TODO: Possibly use the actual Tegra X1 library
    public static byte[] DesizzleNSW(byte[] data, int width, int height, int blockX, int blockY, int bpb)
    {
        var outData = new byte[data.Length];
        var blockWidth = width / blockX;
        var blockHeight = height / blockY;

        var gobsPerBlockX = (blockWidth * bpb + 63) / 64;
        var bpgX = 64;
        var bpgY = 8;

        if (blockX == 1 && blockY == 1)
        {
            bpgY = 16;
            if (blockHeight < 128) bpgY = 8;
        }

        // var gobBytes = bpgX * bpgY;

        if (blockHeight < 64) bpgY = 4;
        if (blockHeight < 32) bpgY = 2;
        if (blockHeight < 16) bpgY = 1;

        for (var dy = 0; dy < blockHeight; dy++)
        {
            for (var dx = 0; dx < blockWidth; dx++)
            {
                var xCoordInBlock = dx * bpb;
                var gobOffset = xCoordInBlock / bpgX * bpgY + dy / (bpgY * 8) * bpgY * gobsPerBlockX + (dy % (bpgY * 8) >> 3);
                gobOffset *= 512;
                var offset = (((xCoordInBlock & 0x3f) >> 5) << 8) + (((dy & 7) >> 1) << 6) + (((xCoordInBlock & 0x1f) >> 4) << 5) + ((dy & 1) << 4) + (xCoordInBlock & 0xf);
                var address = gobOffset + offset;

                if (address >= data.Length) throw new ParserException("Parameters or decoder failed to give proper values");

                Buffer.BlockCopy(data, address, outData, (dy * blockWidth + dx) * bpb, bpb);
            }
        }

        return outData;
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
