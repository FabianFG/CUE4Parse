using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX.Structs.Animations;
using CUE4Parse_Conversion.Writers.UEFormat;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Formats.PoseAsset;

public sealed class UEFormatPoseFormat : IPoseExportFormat
{
    public string DisplayName => "UEFormat (uepose)";
    public string Extension => "uepose";

    public ExportFile Build(string objectName, string objectPath, ExportOptions options, CPoseAsset poseAsset)
    {
        using var Ar = new FArchiveWriter();
        new UEPose(objectName, objectPath, poseAsset, options).Save(Ar);
        return new ExportFile(Extension, Ar.GetBuffer());
    }
}
