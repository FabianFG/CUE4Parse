using CUE4Parse_Conversion.PoseAsset.Conversion;
using CUE4Parse_Conversion.PoseAsset.UEFormat;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Formats.PoseAsset;

public sealed class UEFormatPoseFormat : IPoseExportFormat
{
    public string DisplayName => "UEFormat (uepose)";

    public ExportFile Build(string objectName, ExporterOptions options, CPoseAsset poseAsset)
    {
        using var ar = new FArchiveWriter();
        new UEPose(objectName, poseAsset, options).Save(ar);
        return new ExportFile("uepose", ar.GetBuffer());
    }
}


