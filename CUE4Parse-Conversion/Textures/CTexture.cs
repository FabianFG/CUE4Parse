using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.Textures;

public class CTexture
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Data { get; }

    public EPixelFormat PixelFormat { get; }

    public bool IsFloat => PixelFormat is EPixelFormat.PF_FloatRGB
                                        or EPixelFormat.PF_FloatRGBA
                                        or EPixelFormat.PF_R32_FLOAT
                                        or EPixelFormat.PF_R32G32B32F
                                        or EPixelFormat.PF_A32B32G32R32F;

    public CTexture(int width, int height, EPixelFormat pixelFormat, byte[] data)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        Data = data;
    }
}
