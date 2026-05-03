using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse_Conversion.V2.Exporters;

public interface IExporter
{
    public string PackagePath { get; }
    public string PackageDirectory { get; }
    public string ObjectName { get; }
    public string ObjectPath { get; }
    public string ClassName { get; }

    public Task<IReadOnlyList<ExportResult>> ExportAsync(CancellationToken ct = default);
}

public abstract class ExporterBase : IExporter
{
    public string PackagePath { get; }
    public string PackageDirectory { get; }
    public string ObjectName { get; }
    public string ObjectPath { get; }
    public string ClassName { get; }

    internal ExportSession? _session = null;
    protected ExportSession Session => _session ?? throw new InvalidOperationException("Exporter must be added to an ExportSession before use");

    protected internal ILogger Log { get; }

    protected ExporterBase(UObject export)
    {
        var owner = export.Owner;
        var rawPath = owner?.Name ?? export.GetPathName();

        PackagePath = (owner?.Provider?.FixPath(rawPath) ?? rawPath).SubstringBeforeLast('.');
        PackageDirectory = PackagePath.Contains('/') ? PackagePath.SubstringBeforeLast('/') : string.Empty;
        ObjectName = export.Name;
        ObjectPath = PackagePath + '.' + ObjectName;
        ClassName = export.ExportType;

        Log = Serilog.Log
            .ForContext(nameof(ObjectName), ObjectName)
            .ForContext(nameof(ClassName), ClassName)
            .ForContext("ExporterV2", true);
    }

    protected abstract IReadOnlyList<ExportFile> BuildExportFiles();

    public async Task<IReadOnlyList<ExportResult>> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            var files = BuildExportFiles();
            if (files.Count == 0)
            {
                throw new Exception("Format produced no files");
            }

            var tasks = files.Select(file => WriteExportFileAsync(file, ct));
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to export");
            return [ExportResult.Failure(ObjectPath, ex)];
        }
    }

    private async Task<ExportResult> WriteExportFileAsync(ExportFile file, CancellationToken ct = default)
    {
        var fileName = $"{ObjectName}{file.NameSuffix}.{file.Extension}";
        var path = Session.ResolveOutputPath(GetSavePath(), file.Extension, file.NameSuffix);
        Log.ForContext("FilePath", path).Information("Writing {FileName} ({FileSize} bytes)", fileName, file.Data.Length);

        await File.WriteAllBytesAsync(path, file.Data, ct).ConfigureAwait(false);

        return new ExportResult(true, ObjectPath, path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetSavePath()
    {
        var leaf = PackagePath.SubstringAfterLast('/');
        var path = leaf.Equals(ObjectName, StringComparison.OrdinalIgnoreCase) ? PackagePath : PackagePath + '/' + ObjectName;
        return path.TrimStart('/');
    }

    protected string Resolve(UObject obj, string extension) => Resolve(obj, PackageDirectory, extension);
    internal static string Resolve(UObject obj, string fromDirectory, string extension)
    {
        var owner = obj.Owner;
        var rawPath = owner?.Name ?? obj.GetPathName();
        var packagePath = (owner?.Provider?.FixPath(rawPath) ?? rawPath).SubstringBeforeLast('.');

        // replicate GetSavePath()
        if (!packagePath.SubstringAfterLast('/').Equals(obj.Name, StringComparison.OrdinalIgnoreCase))
        {
            packagePath += '/' + obj.Name;
        }

        if (string.IsNullOrEmpty(fromDirectory))
        {
            return "./" + packagePath.TrimStart('/') + "." + extension;
        }

        var sep = Path.DirectorySeparatorChar;
        var rel = Path.GetRelativePath(
            fromDirectory.Replace('/', sep),
            packagePath.Replace('/', sep)
        ).Replace(sep, '/');

        return (rel.StartsWith("./") || rel.StartsWith("../") ? rel : "./" + rel) + "." + extension;
    }

    public override bool Equals(object? obj) => obj is ExporterBase other && string.Equals(ObjectPath, other.ObjectPath, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => ObjectPath.GetHashCode();
}
