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
}
