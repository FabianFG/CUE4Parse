using CUE4Parse_Conversion.PoseAsset.Conversion;

namespace CUE4Parse_Conversion.V2.Formats.PoseAsset;

public interface IPoseExportFormat : IExportFormat
{
    public ExportFile Build(string objectName, ExporterOptions options, CPoseAsset poseAsset);
}

