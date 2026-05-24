using System.Collections.Generic;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;

namespace CUE4Parse_Conversion.V2.Exporters;

public class RawDataExporter(GameFile file, IFileProvider provider) : ExporterBase(file)
{
    protected override IReadOnlyList<ExportFile> BuildExportFiles()
    {
        var assets = provider.SavePackage(file);

        var result = new List<ExportFile>();
        foreach (var kvp in assets)
        {
            // TODO
            result.Add(new ExportFile(kvp.Key, kvp.Value));
        }

        return result;
    }
}
