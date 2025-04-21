using System;
using System.IO;
using CUE4Parse_Conversion.PoseAsset.UEFormat;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Writers;
using Serilog;

namespace CUE4Parse_Conversion.PoseAsset;

public class PoseAssetExporter : ExporterBase
{
    public PoseAsset PoseAsset;

    public PoseAssetExporter(UPoseAsset poseAsset, ExporterOptions options) : base(poseAsset, options)
    {
        if (!poseAsset.TryConvert(out var convertedPoseAsset))
        {
            Log.Warning($"PoseAsset '{ExportName}' failed to convert");
            return;
        }
        
        using var Ar = new FArchiveWriter();
        string ext;
        switch (Options.PoseFormat)
        {
            case EPoseFormat.UEFormat:
                ext = "uepose";
                new UEPose(poseAsset.Name, convertedPoseAsset, Options).Save(Ar);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Options.PoseFormat), Options.PoseFormat, null);
        }

        PoseAsset = new PoseAsset($"{GetExportSavePath()}.{ext}", Ar.GetBuffer());
    }
    
    public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
    {
        throw new NotImplementedException();
    }

    public override bool TryWriteToZip(out byte[] zipFile)
    {
        throw new NotImplementedException();
    }

    public override void AppendToZip()
    {
        throw new NotImplementedException();
    }
}