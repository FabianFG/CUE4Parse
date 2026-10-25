using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse_Conversion.Exporters;

/// <summary>
/// Keeps enough information to recreate a built-in exporter without keeping its package alive.
/// The original object is preferred while another owner still references it, avoiding a reload for
/// short-lived sessions such as FModel's snooper export.
/// </summary>
internal sealed class DeferredObjectExporter : ExporterBase
{
    private readonly WeakReference<UObject> _export;
    private readonly IFileProvider _provider;
    private readonly GameFile _packageFile;
    private readonly int _exportIndex;
    private readonly bool _hasDependencies;

    // Textures run before the rest when materials are embedded
    public bool IsTexture { get; }

    private DeferredObjectExporter(UObject export, ExporterBase exporter, IFileProvider provider, GameFile packageFile, int exportIndex)
        : base(export)
    {
        _export = new WeakReference<UObject>(export);
        _provider = provider;
        _packageFile = packageFile;
        _exportIndex = exportIndex;
        _hasDependencies = exporter.HasDependencies;
        IsTexture = exporter is TextureExporter;
    }

    internal static DeferredObjectExporter? TryCreate(UObject export, ExporterBase exporter)
    {
        // Subobjects like the components of a level would each load their whole package again, so only assets are deferred
        if (export.Outer is not ResolvedPackageObject || export.Owner is not { Provider: { } provider } owner ||
            !provider.TryGetGameFile(owner.Name, out var packageFile))
        {
            return null;
        }

        // The provider resolves to the copy with the highest priority, which isn't the one that was loaded for patched or
        // explicitly selected archives
        if ((packageFile as VfsEntry)?.Vfs.Name != owner.ArchiveName)
            return null;

        var exportIndex = owner.GetExportIndex(export.Name);
        if (!IsExportAt(owner, exportIndex, export))
        {
            exportIndex = Array.FindIndex(owner.ExportsLazy, candidate => candidate.IsValueCreated && ReferenceEquals(candidate.Value, export));
            if (exportIndex < 0) return null;
        }

        return new DeferredObjectExporter(export, exporter, provider, packageFile, exportIndex);
    }

    private static bool IsExportAt(IPackage package, int index, UObject export) =>
        index >= 0 && index < package.ExportsLazy.Length && package.ExportsLazy[index] is { IsValueCreated: true } candidate &&
        ReferenceEquals(candidate.Value, export);

    private ExporterBase CreateExporter()
    {
        if (!_export.TryGetTarget(out var export))
        {
            var package = _provider.LoadPackage(_packageFile);
            export = package.GetExport(_exportIndex);
            if (export is null || !export.Name.Equals(ObjectName, StringComparison.OrdinalIgnoreCase) || export.ExportType != ClassName)
                throw new InvalidOperationException($"Package '{package.Name}' no longer has {ClassName} '{ObjectName}' at export index {_exportIndex}.");

            _export.SetTarget(export);
        }

        var exporter = ExportSession.CreateExporter(export);
        exporter._session = _session;
        return exporter;
    }

    // The object is only needed while its dependencies are read, it can be collected again until it's exported
    internal override bool HasDependencies => _hasDependencies;
    internal override IEnumerable<UObject> GetDependencies(CancellationToken ct) =>
        _hasDependencies ? CreateExporter().GetDependencies(ct).ToList() : [];

    public override async Task<ExportResult> ExportAsync(CancellationToken ct = default)
    {
        ExporterBase exporter;
        try
        {
            ct.ThrowIfCancellationRequested();
            exporter = CreateExporter();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to load deferred export");
            return ExportResult.Failure(ObjectPath, ex);
        }

        return await exporter.ExportAsync(ct).ConfigureAwait(false);
    }

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default) =>
        throw new NotSupportedException();
}
