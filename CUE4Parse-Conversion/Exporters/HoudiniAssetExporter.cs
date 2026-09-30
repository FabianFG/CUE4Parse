using CUE4Parse.UE4.Assets.Exports.Houdini;

namespace CUE4Parse_Conversion.Exporters;

public sealed class HoudiniAssetExporter(UHoudiniAsset asset) : ExporterBase(asset)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        if (asset.HdaBuffer.Length == 0)
            throw new Exception("Houdini asset contains no data");

        return [new ExportFile("hda", asset.HdaBuffer)];
    }
}
