using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Dto;

namespace CUE4Parse_Conversion.Exporters;

public sealed class EmbeddedFilesExporter(UObject export) : ExporterBase(export)
{
    private readonly UObject _export = export;

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default) => [.. BuildFiles(ct)];

    public override async Task<ExportResult> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            var paths = new List<string>();
            // Large wwise asset libraries can contain thousands of banks/media files. Read and write one at a time
            foreach (var file in BuildFiles(ct))
            {
                paths.Add(await WriteExportFileAsync(file, ct).ConfigureAwait(false));
            }

            if (paths.Count == 0)
                throw new InvalidOperationException("Asset contains no embedded files");

            return new ExportResult(true, ObjectPath, paths);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to export embedded files");
            return ExportResult.Failure(ObjectPath, ex);
        }
    }

    private IEnumerable<ExportFile> BuildFiles(CancellationToken ct)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in new EmbeddedFilesDto(_export).Files)
        {
            ct.ThrowIfCancellationRequested();
            var data = file.ReadData();
            if (data.Length == 0)
                continue;
            
            var name = file.Name.SanitizePath();
            if (string.IsNullOrWhiteSpace(name))
                name = ObjectName;

            // So we don't overwrite files with the same name
            var uniqueName = name;
            var index = 2;
            while (names.Contains($"{uniqueName}.{file.Extension}"))
            {
                uniqueName = $"{name}_{index}";
                index++;
            }
            names.Add($"{uniqueName}.{file.Extension}");

            yield return new ExportFile(file.Extension, data, uniqueName);
        }
    }

    protected override (string, string) ResolveOutputPath(ExportFile file)
    {
        var name = file.NameSuffix ?? ObjectName;
        var path = Session.ResolveOutputPath(Path.Combine(SaveDirectory, name), file.Extension);
        return ($"{name}.{file.Extension}", path);
    }
}
