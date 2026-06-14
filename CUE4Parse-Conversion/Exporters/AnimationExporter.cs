using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Exporters;

public sealed class AnimationExporter(UAnimationAsset animation) : ExporterBase(animation)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Converting animation to {Format}", Session.Options.MeshFormat);

        var format = GetAnimFormat(Session.Options.MeshFormat);
        return format.Build(ObjectName, Session.Options, animation.ConvertAnims());
    }

    private IAnimExportFormat GetAnimFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => new ActorXAnimFormat(),
        EMeshFormat.UEFormat => new UEFormatAnimFormat(),
        EMeshFormat.USD => new UsdAnimFormat(),
        _ => throw new NotSupportedException($"Animation export does not support format {format}. Available formats: {string.Join(", ", "ActorX", "UEFormat", "USD")}"),
    };
}
