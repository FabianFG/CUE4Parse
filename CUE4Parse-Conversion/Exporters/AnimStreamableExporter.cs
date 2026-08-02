using System.Collections.Generic;
using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Exporters;

public sealed class AnimStreamableExporter(UAnimStreamable animStreamable) : AnimationExporter<UAnimStreamable>(animStreamable)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UAnimStreamable animStreamable, IAnimExportFormat format)
        => format.BuildAnimStreamable(ObjectName, Session.Options, animStreamable);
}
