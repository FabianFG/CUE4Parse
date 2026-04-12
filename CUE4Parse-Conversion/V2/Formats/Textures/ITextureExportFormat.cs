using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.V2.Formats.Textures;

public interface ITextureExportFormat : IExportFormat
{
    public ExportFile Build(CTexture texture, bool saveHdrAsHdr);
}

