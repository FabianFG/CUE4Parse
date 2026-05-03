using CUE4Parse_Conversion.V2.Dto.World;

namespace CUE4Parse_Conversion.V2.Formats.World;

public class UEFormatWorldFormat : IWorldExportFormat
{
    public string DisplayName => "UEFormat (ueworld)";

    public ExportFile Build(WorldDto dto, WorldAssetPaths paths)
    {
        throw new System.NotImplementedException();
    }
}
