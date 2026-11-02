using System.ComponentModel;

namespace CUE4Parse_Conversion.Options;

public enum EAnimatedTextureFormat
{
    [Description("PNG (animated)")]
    Png,
    [Description("WebP (animated)")]
    Webp,
    [Description("GIF")]
    Gif,
    [Description("Auto")] // Selects based on ETextureFormat, if given format supports animation, otherwise PNG
    Auto
}
