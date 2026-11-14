using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse_Conversion.Textures.Paper2D;

namespace CUE4Parse_Conversion.Exporters.Paper2D;

public sealed class FlipbookExporter(UPaperFlipbook flipbook) : ExporterBase(flipbook)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        var decoded = flipbook.Decode(Session.Options, cancellationToken: ct) ?? throw new Exception("Failed to decode flipbook");
        var data = decoded.Encode(Session.Options, out var ext, ct);
        return [new ExportFile(ext, data)];
    }
}
