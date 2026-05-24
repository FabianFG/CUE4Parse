using CUE4Parse_Conversion.V2.Options;
using CUE4Parse_Conversion.V2.Writers.ActorX.Structs.Animations;

namespace CUE4Parse_Conversion.V2.Formats.PoseAsset;

public interface IPoseExportFormat : IExportFormat
{
    public ExportFile Build(string objectName, ExportOptions options, CPoseAsset poseAsset);
}

