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

    public Task<IReadOnlyList<ExportResult>> ExportAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default);
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

    protected ILogger Log { get; }

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

    public abstract Task<IReadOnlyList<ExportResult>> ExportAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default);

    protected async Task<ExportResult> WriteExportFileAsync(ExportFile file, IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        var fileName = $"{ObjectName}{file.NameSuffix}.{file.Extension}";
        var path = ResolveOutputPath(file.Extension, file.NameSuffix);
        Log.ForContext("FilePath", path).Verbose("Writing {FileName} ({FileSize} bytes)", fileName, file.Data.Length);

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

    private string ResolveOutputPath(string ext, string nameSuffix = "")
    {
        var savePath = GetSavePath();
        var fullPath = Path.Combine(Session.BaseDirectory.FullName, savePath) + nameSuffix + '.' + ext.ToLower();

        var dir = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException($"Cannot determine directory for path: {fullPath}");
        Directory.CreateDirectory(dir);
        return fullPath.Replace('/', '\\');
    }

    public override bool Equals(object? obj) => obj is ExporterBase2 other && string.Equals(ObjectPath, other.ObjectPath, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode()
    {
        return ObjectPath.GetHashCode();
    }
}
