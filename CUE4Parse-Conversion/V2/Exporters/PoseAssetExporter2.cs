using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.PoseAsset;
using CUE4Parse_Conversion.V2.Formats.PoseAsset;
using CUE4Parse.UE4.Objects.Engine.Animation;

namespace CUE4Parse_Conversion.V2.Exporters;

public class PoseAssetExporter2(UPoseAsset poseAsset) : ExporterBase2(poseAsset)
{
    protected override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        Log.Debug("Converting pose asset to {Format}", Session.Options.PoseFormat);

        if (!poseAsset.TryConvert(out var convertedPoseAsset))
        {
            throw new Exception("Failed to convert");
        }

        var format = GetPoseFormat(Session.Options.PoseFormat);
        var file = format.Build(ObjectName, Session.Options, convertedPoseAsset);

        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }

    private IPoseExportFormat GetPoseFormat(EPoseFormat format) => format switch
    {
        EPoseFormat.UEFormat => new UEFormatPoseFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported pose format")
    };
}
