using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;

namespace CUE4Parse_Conversion.Formats.PoseAsset;

public interface IPoseExportFormat : IExportFormat
{
    public ExportFile Build(string objectName, ExportOptions options, CPoseAsset poseAsset);
}

