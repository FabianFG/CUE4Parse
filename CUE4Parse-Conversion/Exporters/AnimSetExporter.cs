using System.Collections.Generic;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Exporters;

public sealed class AnimSetExporter(UAnimationAsset animation) : AnimationExporter<UAnimationAsset>(animation)
{
    protected override IReadOnlyList<ExportFile> BuildFiles(UAnimationAsset animation, IAnimExportFormat format)
        => format.BuildAnimation(ObjectName, Session.Options, animation.ConvertAnims());
}
