using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.V2.Formats.Animations;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class AnimationExporter2(UAnimationAsset animation) : ExporterBase2(animation)
{
    public override async Task<IReadOnlyList<ExportResult>> ExportAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        Log.Debug("Converting animation to {Format}", Session.Options.AnimFormat);

        var format = GetAnimFormat(Session.Options.AnimFormat);
        var files = format.Build(ObjectName, Session.Options, animation.ConvertAnims());

        if (files.Count == 0)
        {
            return [ExportResult.Failure(ObjectName, PackagePath, PackageDirectory, new Exception("Format produced no files"))];
        }

        var tasks = files.Select(file => WriteExportFileAsync(file, progress, ct)).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    private IAnimExportFormat GetAnimFormat(EAnimFormat format) => format switch
    {
        EAnimFormat.ActorX => new ActorXAnimFormat(),
        EAnimFormat.UEFormat => new UEAnimExportFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported animation format")
    };
}
