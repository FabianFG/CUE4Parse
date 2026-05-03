using System.ComponentModel;

namespace CUE4Parse_Conversion.V2.Options;

public enum ETextureFormat
{
    [Description("PNG")]
    Png,
    [Description("JPEG")]
    Jpeg,
    [Description("TGA")]
    Tga,
    [Description("DDS (Not Implemented)")]
    Dds,
    [Description("WebP")]
    Webp,
}
