using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.V2.Formats.Textures;

public sealed class PngTextureFormat : ITextureExportFormat
{
    public string DisplayName => "PNG";

    public ExportFile Build(CTexture texture, bool saveHdrAsHdr)
    {
        var data = texture.Encode(ETextureFormat.Png, saveHdrAsHdr, out var ext);
        return new ExportFile(ext, data);
    }
}

public sealed class JpegTextureFormat : ITextureExportFormat
{
    public string DisplayName => "JPEG";

    public ExportFile Build(CTexture texture, bool saveHdrAsHdr)
    {
        var data = texture.Encode(ETextureFormat.Jpeg, saveHdrAsHdr, out var ext);
        return new ExportFile(ext, data);
    }
}

public sealed class TgaTextureFormat : ITextureExportFormat
{
    public string DisplayName => "TGA";

    public ExportFile Build(CTexture texture, bool saveHdrAsHdr)
    {
        var data = texture.Encode(ETextureFormat.Tga, saveHdrAsHdr, out var ext);
        return new ExportFile(ext, data);
    }
}
