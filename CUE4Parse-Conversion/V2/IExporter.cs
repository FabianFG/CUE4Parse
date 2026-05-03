using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse_Conversion.V2;

public interface IExporter2
{
    public string PackagePath { get; }
    public string PackageDirectory { get; }
    public string ObjectName { get; }
    public string ObjectPath { get; }
    public string ClassName { get; }

    public Task<IReadOnlyList<ExportResult>> ExportAsync(CancellationToken ct = default);
}

public abstract class ExporterBase2 : IExporter2
{
    public string PackagePath { get; }
    public string PackageDirectory { get; }
    public string ObjectName { get; }
    public string ObjectPath { get; }
    public string ClassName { get; }

    internal ExportSession? _session = null;
    protected ExportSession Session => _session ?? throw new InvalidOperationException("Exporter must be added to an ExportSession before use");

    protected internal ILogger Log { get; }

    protected ExporterBase2(UObject export)
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

    protected abstract Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default);
    public async Task<IReadOnlyList<ExportResult>> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            return await DoExportAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to export");
            return [ExportResult.Failure(ObjectPath, ex)];
        }
    }

    protected async Task<ExportResult> WriteExportFileAsync(ExportFile file, CancellationToken ct = default)
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

    public override bool Equals(object? obj) => obj is ExporterBase2 other && string.Equals(ObjectPath, other.ObjectPath, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => ObjectPath.GetHashCode();
}
