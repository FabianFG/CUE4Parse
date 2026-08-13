using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports.Animation;

namespace CUE4Parse_Conversion.Exporters;

public sealed class AnimationExporter(UAnimationAsset animation) : ExporterBase(animation)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Converting animation to {Format}", Session.Options.MeshFormat);

        var format = GetAnimFormat(Session.Options.MeshFormat);
        return format switch
        {
            // TODO: should we extend IAnimExportFormat to handle these for all writers?
            UEFormatAnimFormat when animation is UAnimStreamable streamable => new UEFormatAnimFormat().BuildAnimStreamable(ObjectName, ObjectPath, Session.Options, streamable),
            _ => format.Build(ObjectName, ObjectPath, Session.Options, animation.ConvertAnims())
        };
    }

    private IAnimExportFormat GetAnimFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => new ActorXAnimFormat(),
        EMeshFormat.UEFormat => new UEFormatAnimFormat(),
        EMeshFormat.USD => new UsdAnimFormat(),
        _ => throw new NotSupportedException($"Animation export does not support format {format}. Available formats: {string.Join(", ", "ActorX", "UEFormat", "USD")}"),
    };
}
