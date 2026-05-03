using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.PoseAsset;
using CUE4Parse_Conversion.V2.Formats.PoseAsset;
using CUE4Parse_Conversion.V2.Options;
using CUE4Parse.UE4.Objects.Engine.Animation;

namespace CUE4Parse_Conversion.V2.Exporters;

public class PoseAssetExporter(UPoseAsset poseAsset) : ExporterBase(poseAsset)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles()
    {
        Log.Debug("Converting pose asset to {Format}", Session.Options.MeshFormat);

        if (!poseAsset.TryConvert(out var convertedPoseAsset))
        {
            throw new Exception("Failed to convert");
        }

        var format = GetPoseFormat(Session.Options.MeshFormat);
        return [format.Build(ObjectName, Session.Options, convertedPoseAsset)];
    }

    private IPoseExportFormat GetPoseFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.UEFormat => new UEFormatPoseFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported pose format")
    };
}
