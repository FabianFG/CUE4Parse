using CUE4Parse_Conversion.V2.Dto.World;

namespace CUE4Parse_Conversion.V2.Formats.World;

public interface IWorldExportFormat : IExportFormat
{
    public ExportFile Build(WorldDto dto, WorldAssetPaths paths);
}
