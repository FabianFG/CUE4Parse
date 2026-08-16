using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports;

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

    private DeferredObjectExporter(UObject export, IFileProvider provider, GameFile packageFile, int exportIndex)
        : base(export)
    {
        _export = new WeakReference<UObject>(export);
        _provider = provider;
        _packageFile = packageFile;
        _exportIndex = exportIndex;
    }

    internal static DeferredObjectExporter? TryCreate(UObject export)
    {
        if (export.Owner is not { Provider: { } provider } owner ||
            !provider.TryGetGameFile(owner.Name, out var packageFile))
        {
            return null;
        }

        for (var exportIndex = 0; exportIndex < owner.ExportsLazy.Length; exportIndex++)
        {
            var candidate = owner.ExportsLazy[exportIndex];
            if (!candidate.IsValueCreated || !ReferenceEquals(candidate.Value, export)) continue;

            return new DeferredObjectExporter(export, provider, packageFile, exportIndex);
        }

        return null;
    }

    public override async Task<ExportResult> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_export.TryGetTarget(out var export))
            {
                var package = await _provider.LoadPackageAsync(_packageFile).ConfigureAwait(false);
                export = package.GetExport(_exportIndex) ??
                         throw new InvalidOperationException($"Package '{package.Name}' no longer has export index {_exportIndex}.");
                _export.SetTarget(export);
            }

            var exporter = ExportSession.CreateExporter(export);
            exporter._session = _session;
            return await exporter.ExportAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error(ex, "Failed to load deferred export");
            return ExportResult.Failure(ObjectPath, ex);
        }
    }

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default) =>
        throw new NotSupportedException();
}
