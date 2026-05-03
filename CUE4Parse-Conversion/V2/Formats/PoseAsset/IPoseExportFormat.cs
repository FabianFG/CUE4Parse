using CUE4Parse_Conversion.PoseAsset.Conversion;
using CUE4Parse_Conversion.V2.Options;

namespace CUE4Parse_Conversion.V2.Formats.PoseAsset;

public interface IPoseExportFormat : IExportFormat
{
    public ExportFile Build(string objectName, ExportOptions options, CPoseAsset poseAsset);
}

