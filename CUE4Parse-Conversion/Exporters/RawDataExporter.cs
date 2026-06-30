using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.Exporters;

/// <summary>
/// hacky raw data exporter just to delegate the work to our new export session thing
/// instead of letting the user do the dirty work
/// </summary>
public sealed class RawDataExporter(GameFile gameFile, IFileProvider provider) : ExporterBase(gameFile, "RawData")
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        var assets = provider.SavePackage(gameFile);

        var result = new List<ExportFile>();
        foreach (var kvp in assets)
        {
            result.Add(new ExportFile(kvp.Key, kvp.Value));
        }

        return result;
    }

    protected override (string, string) ResolveOutputPath(ExportFile file)
    {
        var parts = file.Extension.Split('.');
        var path = Session.ResolveOutputPath(parts[0], parts[1], file.NameSuffix);
        return (file.Extension.SubstringAfterLast('/'), path);
    }
}
