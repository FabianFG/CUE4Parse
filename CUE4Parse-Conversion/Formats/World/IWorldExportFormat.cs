using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Formats.World;

public interface IWorldExportFormat : IExportFormat
{
    public ExportFile Build(WorldDto dto, WorldAssetPaths paths);
}
