using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse_Conversion.Exporters;

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

    private ExporterBase(string packagePath, string objectName, string className)
    {
        PackagePath = packagePath;
        PackageDirectory = PackagePath.Contains('/') ? PackagePath.SubstringBeforeLast('/') : string.Empty;
        ObjectName = objectName;
        ObjectPath = PackagePath + '.' + ObjectName;
        ClassName = className;

        // TODO: ObjectName is not unique across packages and their objects, so multiple distinct packages can share the same name
        // in such case it won't be possible to distinguish the two without putting ObjectPath as context
        // it should mostly be fine tho as the session is still gonna export the two but filtering logs will be a bit more difficult
        // Known cases:
        //   FarFarWest/Content/Interfaces/MainMenu/UI_LoadingScreen.UI_LoadingScreen
        //   FarFarWest/Content/Interfaces/UI_LoadingScreen.UI_LoadingScreen
        //   FarFarWest/Content/Interfaces/Textures/T_ContractBackground.T_ContractBackground
        //   FarFarWest/Content/Interfaces/Contracts/T_ContractBackground.T_ContractBackground
        Log = Serilog.Log.ForContext(GetType())
            .ForContext(nameof(ObjectName), ObjectName)
            .ForContext(nameof(ClassName), ClassName)
            .ForContext("ExporterV2", true);
    }

    protected ExporterBase(UObject export) : this(BuildPackagePath(export), export.Name, export.ExportType)
    {

    }

    protected internal ExporterBase(GameFile file) : this(file.PathWithoutExtension, file.NameWithoutExtension, "RawData")
    {
        if (file.IsUePackagePayload)
            throw new ArgumentException("GameFile must not be a UE package payload file", nameof(file));
    }

    protected abstract IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default);

    public async Task<IReadOnlyList<ExportResult>> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            var files = BuildExportFiles(ct);
            if (files.Count == 0)
            {
                throw new Exception("Format produced no files");
            }

            var tasks = files.Select(file => WriteExportFileAsync(file, ct));
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed: {Message}", ex.Message);
            return [ExportResult.Failure(ObjectPath, ex)];
        }
    }

    private async Task<ExportResult> WriteExportFileAsync(ExportFile file, CancellationToken ct = default)
    {
        var (fileName, path) = ResolveOutputPath(file); // fileName may not be the real file name, it's just for logging
        Log.ForContext("FilePath", path).Information("Writing {FileName} ({FileSize} bytes)", fileName, file.Data.Length);

        await File.WriteAllBytesAsync(path, file.Data, ct).ConfigureAwait(false);

        return new ExportResult(true, ObjectPath, path);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual (string, string) ResolveOutputPath(ExportFile file)
    {
        return ($"{ObjectName}{file.NameSuffix}.{file.Extension}", Session.ResolveOutputPath(GetSavePath(), file.Extension, file.NameSuffix));
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
        var packagePath = BuildPackagePath(obj);

        // replicate GetSavePath()
        if (!packagePath.SubstringAfterLast('/').Equals(obj.Name, StringComparison.OrdinalIgnoreCase))
            packagePath += '/' + obj.Name;

        return MakeRelativeRef(packagePath.TrimStart('/'), fromDirectory, extension);
    }

    /// <summary>
    /// Computes the package-scoped output path for a UObject, folding any outer-object hierarchy
    /// into the path as subfolders.
    /// <para>
    /// UE subobjects carry a <c>':'</c> in their path name that separates the package-level export
    /// from the subobject chain, e.g. <c>/Game/Maps/Level.PersistentLevel:MyActor.MyComp</c>.
    /// Each outer name (excluding the object itself) becomes a folder, so components inside
    /// different actors never share the same output path while top-level assets are unaffected.
    /// </para>
    /// </summary>
    private static string BuildPackagePath(UObject obj)
    {
        var owner = obj.Owner;
        var pathName = obj.GetPathName();
        var rawPath = owner?.Name ?? pathName;
        var basePath = (owner?.Provider?.FixPath(rawPath) ?? rawPath).SubstringBeforeLast('.');

        if (pathName.IndexOf(':') is > 0 and var colonIdx)
        {
            var subChain = pathName[(colonIdx + 1)..]; // e.g. "MyActor.MyComp"
            var lastDot = subChain.LastIndexOf('.');
            if (lastDot > 0)
            {
                basePath += '/' + subChain[..lastDot].Replace('.', '/'); // "MyActor" or "A/B/C"
            }
        }

        return basePath;
    }

    private static string MakeRelativeRef(string savePath, string fromDirectory, string extension)
    {
        if (string.IsNullOrEmpty(fromDirectory))
        {
            return "./" + savePath + "." + extension;
        }

        var sep = Path.DirectorySeparatorChar;
        var rel = Path.GetRelativePath(
            fromDirectory.Replace('/', sep),
            savePath.Replace('/', sep)
        ).Replace(sep, '/');

        return (rel.StartsWith("./") || rel.StartsWith("../") ? rel : "./" + rel) + "." + extension;
    }

    public override bool Equals(object? obj) => obj is ExporterBase other && string.Equals(ObjectPath, other.ObjectPath, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => ObjectPath.GetHashCode();
}
