using System.ComponentModel;

namespace CUE4Parse_Conversion.Textures
{
    public enum ETextureFormat
    {
        [Description("PNG")]
        Png = 0,
        [Description("JPEG")]
        Jpeg = 1,
        [Description("WebP")]
        Webp = 4,
        [Description("TGA")]
        Tga = 2,
        [Description("DDS (Not Implemented)")]
        Dds = 3
    }
}
