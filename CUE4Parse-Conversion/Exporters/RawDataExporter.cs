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
    private readonly GameFile _gameFile = gameFile;

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        var assets = provider.SavePackage(_gameFile);

        var result = new List<ExportFile>();
        foreach (var kvp in assets)
        {
            result.Add(new ExportFile(kvp.Key, kvp.Value));
        }

        return result;
    }

    protected override (string, string) ResolveOutputPath(ExportFile file)
    {
        var name = Path.GetFileName(file.Extension);
        var extension = Path.GetExtension(file.Extension);
        var savePath = Path.ChangeExtension(file.Extension, null);

        var path = Session.ResolveOutputPath(savePath, extension.TrimStart('.'), file.NameSuffix);
        return (name, path);
    }
}
