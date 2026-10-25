using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.Textures;

public class CTexture
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Data { get; }
    public EPixelFormat PixelFormat { get; }

    public CTexture(int width, int height, EPixelFormat pixelFormat, byte[] data)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Data = data;
    }

    public CTexture Crop(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);

        if (x + width > Width || y + height > Height)
            throw new ArgumentOutOfRangeException("Crop region is outside the texture bounds.");

        if (x == 0 && y == 0 && width == Width && height == Height)
            return this;

        var bpp = PixelFormatUtils.PixelFormats[PixelFormat].BlockBytes;
        var croppedData = new byte[width * height * bpp];
        var rowBytes = width * bpp;
        var srcStride = Width * bpp;

        for (var row = 0; row < height; row++)
        {
            var srcOffset = (y + row) * srcStride + (x * bpp);
            var dstOffset = row * rowBytes;
            Buffer.BlockCopy(Data, srcOffset, croppedData, dstOffset, rowBytes);
        }

        return new CTexture(width, height, PixelFormat, croppedData);
    }
}
