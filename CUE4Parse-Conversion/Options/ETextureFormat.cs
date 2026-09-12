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
	[Description("TGA (Compressed)")]
	TgaRle,
    [Description("WebP")]
    Webp,
}
