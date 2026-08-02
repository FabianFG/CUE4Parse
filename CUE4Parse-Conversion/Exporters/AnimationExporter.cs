using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse_Conversion.Exporters;

public abstract class AnimationExporter<T>(T animation) : ExporterBase(animation) where T : UObject
{
    protected abstract IReadOnlyList<ExportFile> BuildFiles(T original, IAnimExportFormat format);

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Converting animation to {Format}", Session.Options.MeshFormat);

        return BuildFiles(animation, GetAnimFormat(Session.Options.MeshFormat));
    }

    private IAnimExportFormat GetAnimFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => new ActorXAnimFormat(),
        EMeshFormat.UEFormat => new UEFormatAnimFormat(),
        EMeshFormat.USD => new UsdAnimFormat(),
        _ => throw new NotSupportedException($"Animation export does not support format {format}. Available formats: {string.Join(", ", "ActorX", "UEFormat", "USD")}"),
    };
}
