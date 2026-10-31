using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse_Conversion.Exporters;

public sealed class FlipbookExporter(UPaperFlipbook flipbook) : ExporterBase(flipbook)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        Log.Debug("Decoding flipbook for platform {Platform} as {Format}", Session.Options.TexturePlatform, Session.Options.TextureFormat);

        var decoded = flipbook.Decode(Session.Options.TexturePlatform, cancellationToken: ct)
            ?? throw new Exception("Failed to decode flipbook");

        var data = decoded.Encode(Session.Options, out var ext, ct);
        return [new ExportFile(ext, data)];
    }
}
