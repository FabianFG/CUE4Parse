using System.ComponentModel;

namespace CUE4Parse_Conversion.Options;

public enum ETextureFormat
{
    [Description("PNG")]
    Png,
    [Description("JPEG")]
    Jpeg,
    [Description("TGA")]
    Tga,
    [Description("WebP")]
    Webp,
}
