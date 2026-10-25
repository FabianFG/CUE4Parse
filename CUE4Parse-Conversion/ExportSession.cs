using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.GeometryCollection;
using CUE4Parse.UE4.Assets.Exports.Houdini;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Exporters;
using CUE4Parse_Conversion.Exporters.Custom;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion;

public sealed class ExportSession(Action<StreamingLevelFilterArgs, CancellationToken>? streamingLevelFilter = null) : INotifyPropertyChanged
{
    private ExportSessionCache? _cache;
    public ExportSessionCache Cache => _cache ?? throw new InvalidOperationException("Session is not currently running.");

    internal ExportSession(ExportOptions options) : this((Action<StreamingLevelFilterArgs, CancellationToken>?) null)
    {
        _options = options;
        _cache = new ExportSessionCache();
    }

    internal readonly Action<StreamingLevelFilterArgs, CancellationToken>? _streamingLevelFilter = streamingLevelFilter;
    internal readonly SemaphoreSlim _streamingLevelFilterLock = new(1, 1);

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    private DirectoryInfo? _baseDirectory;
    internal DirectoryInfo BaseDirectory => _baseDirectory ?? throw new InvalidOperationException("Session is not currently running.");

    private ExportOptions? _options;
    internal ExportOptions Options => _options ?? throw new InvalidOperationException("Session is not currently running.");

    public bool EmbedMaterials { get; private set; }

    public PbrMaterialDto ResolveMaterial(UMaterialInterface material, EMaterialDepth? depth = null)
        => Cache.ResolveMaterial(material, depth ?? Options.MaterialDepth);

    private int _totalQueued;
    public int TotalQueued => Volatile.Read(ref _totalQueued);
    public bool HasQueuedItems => TotalQueued > 0;

    private int _running;
    public bool IsRunning => Volatile.Read(ref _running) == 1;

    // Object paths in the order they were added. The exporters wait in _pending until they're processed or removed,
    // and _paths keeps an object from being exported twice in one run.
    private readonly ConcurrentQueue<string> _order = new();
    private readonly ConcurrentDictionary<string, ExporterBase> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _paths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _stateLock = new();

    /// <summary>
    /// Queues the built-in exporter for an object. Objects loaded from a file provider are only held weakly until
    /// they're exported and get loaded again if they were collected in the meantime, so a large queue doesn't keep
    /// every package alive. Changes made to such an object are lost when it's reloaded, so use
    /// <see cref="Add(ExporterBase)"/> to export the exact in-memory object.
    /// </summary>
    public ExportSession Add(UObject export)
    {
        var exporter = CreateExporter(export);
        if (_paths.ContainsKey(exporter.ObjectPath)) return this;

        return Add(DeferredObjectExporter.TryCreate(export, exporter) ?? exporter);
    }

    internal static ExporterBase CreateExporter(UObject export) => export switch
    {
        UTexture texture => new TextureExporter(texture),
        UMaterialInterface material => new MaterialExporter(material),
        USkinnedAsset skinnedAsset => new SkinnedAssetExporter(skinnedAsset),
        UStaticMesh staticMesh => new StaticMeshExporter(staticMesh),
        UHoudiniStaticMesh houdiniMesh => new HoudiniStaticMeshExporter(houdiniMesh),
        UIRMesh irMesh => new IRMeshExporter(irMesh),
        UGeometryCollection geometryCollection => new GeometryCollectionExporter(geometryCollection),
        USkeleton skeleton => new SkeletonExporter(skeleton),
        UPoseAsset poseAsset => new PoseAssetExporter(poseAsset),
        UAnimationAsset animation => new AnimationExporter(animation),
        UDNAAsset dna => new DnaExporter(dna),
        UWorld world => new WorldExporter(world),
        UBlueprintGeneratedClass blueprint => new BlueprintExporter(blueprint),
        UPaperTileMap map when map.Game is EGame.GAME_DeadByDaylight => new DeadByDaylightMapExporter(map),
        ALandscapeProxy landscape => new LandscapeMeshExporter(landscape),
        ULandscapeComponent landscape => new LandscapeMeshExporter2(landscape),
        USplineMeshComponent spline => new SplineMeshExporter(spline),
        _ when EmbeddedFilesDto.Supports(export) => new EmbeddedFilesExporter(export),
        _ => throw new NotSupportedException($"Could not create exporter for export of type '{export.GetType().Name}'.")
    };

    public ExportSession Add(ExporterBase exporter)
    {
        lock (_stateLock)
        {
            // TODO: this prevents 2 exporters messing with the same file from being enqueued in the same run (e.g. MeshExporter / RawDataExporter)
            if (!_paths.TryAdd(exporter.ObjectPath, 0)) return this;

            exporter._session = this;
            Interlocked.Increment(ref _totalQueued);
            _pending[exporter.ObjectPath] = exporter;
            _order.Enqueue(exporter.ObjectPath);
        }

        OnPropertyChanged(nameof(TotalQueued));
        OnPropertyChanged(nameof(HasQueuedItems));
        exporter.Log.Debug("Queued for export");
        return this;
    }

    public bool Remove(string objectPath)
    {
        lock (_stateLock)
        {
            if (IsRunning || !_paths.TryRemove(objectPath, out _))
                return false;

            // the path stays in _order, it's skipped once its exporter is gone
            _pending.TryRemove(objectPath, out _);
        }

        Interlocked.Decrement(ref _totalQueued);
        OnPropertyChanged(nameof(TotalQueued));
        OnPropertyChanged(nameof(HasQueuedItems));
        return true;
    }

    public bool Clear()
    {
        lock (_stateLock)
        {
            // Workers that are still running would count down from zero, like Remove a running session is left alone
            if (IsRunning)
                return false;

            ClearQueue();
        }

        OnPropertyChanged(nameof(TotalQueued));
        OnPropertyChanged(nameof(HasQueuedItems));
        return true;
    }

    private void ClearQueue()
    {
        _order.Clear();
        _pending.Clear();
        _paths.Clear();
        Interlocked.Exchange(ref _totalQueued, 0);
    }

    public async Task<IReadOnlyList<ExportResult>> RunAsync(string baseDirectory, ExportOptions options, IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        lock (_stateLock)
        {
            if (Interlocked.Exchange(ref _running, 1) == 1)
                throw new InvalidOperationException("Session is already running.");
        }

        var results = new List<ExportResult>();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = ct };
        try
        {
            OnPropertyChanged(nameof(IsRunning));
            _baseDirectory = new DirectoryInfo(baseDirectory);
            _options = options;
            _cache = new ExportSessionCache();

            // Only glTF embeds materials as of now.
            EmbedMaterials = options.MeshFormat is EMeshFormat.Gltf2 && options.ExportMaterials;

            var current = new List<ExporterBase?>();
            while (true)
            {
                current.Clear();
                DrainQueue(current, ct);
                if (current.Count == 0)
                    break;

                // Textures are processed first so embedded materials can reuse saved files
                if (EmbedMaterials)
                {
                    CollectDependencies(current, parallelOptions);
                    var textures = current.Where(IsTextureExporter).ToList();
                    current.RemoveAll(IsTextureExporter);
                    await ProcessBatch(textures).ConfigureAwait(false);
                }

                await ProcessBatch(current).ConfigureAwait(false);
            }
        }
        finally // just in case cancellation is requested, we still need to clear things up
        {
            var stillQueued = TotalQueued;
            int count;
            lock (results) count = results.Count;

            _options = null;
            _baseDirectory = null;
            EmbedMaterials = false;
            _cache?.Clear();
            _cache = null;
            lock (_stateLock)
            {
                Interlocked.Exchange(ref _running, 0);

                // Release session data before notifying callers, which may throw
                ClearQueue();
            }

            OnPropertyChanged(nameof(TotalQueued));
            OnPropertyChanged(nameof(HasQueuedItems));
            progress?.Report(new ExportProgress(count, count + stillQueued)); // this ensure the last progress reports the actual numbers
            OnPropertyChanged(nameof(IsRunning));
        }

        lock (results) return [.. results];

        // Each exporter leaves its batch when it starts, so finished ones can be collected while the batch goes on
        Task ProcessBatch(List<ExporterBase?> batch) => Parallel.ForEachAsync(Enumerable.Range(0, batch.Count), parallelOptions, (index, token) =>
        {
            var exporter = batch[index]!;
            batch[index] = null;
            return Process(exporter, token);
        });

        async ValueTask Process(ExporterBase exporter, CancellationToken token)
        {
            var result = await exporter.ExportAsync(token).ConfigureAwait(false);

            // Counting and reporting together keeps the progress in order while workers finish concurrently
            lock (results)
            {
                results.Add(result);
                var count = results.Count;
                var stillQueued = Interlocked.Decrement(ref _totalQueued);
                progress?.Report(new ExportProgress(count, count + stillQueued, result));
            }

            OnPropertyChanged(nameof(TotalQueued));
            OnPropertyChanged(nameof(HasQueuedItems));
        }
    }

    private static bool IsTextureExporter(ExporterBase? exporter) => exporter is TextureExporter or DeferredObjectExporter { IsTexture: true };

    // Appended materials can add textures, visit them before processing batch
    private void CollectDependencies(List<ExporterBase?> exporters, ParallelOptions options)
    {
        var start = 0;
        while (start < exporters.Count)
        {
            var end = exporters.Count;
            Parallel.For(start, end, options, i =>
            {
                var exporter = exporters[i]!;
                try
                {
                    foreach (var dependency in exporter.GetDependencies(options.CancellationToken))
                    {
                        Add(dependency);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    exporter.Log.Warning(ex, "Could not collect export dependencies");
                }
            });

            start = end;
            DrainQueue(exporters, options.CancellationToken);
        }
    }

    private void DrainQueue(List<ExporterBase?> exporters, CancellationToken ct)
    {
        while (_order.TryDequeue(out var path))
        {
            ct.ThrowIfCancellationRequested();
            if (_pending.TryRemove(path, out var exporter)) // missing = exporter was added but then manually removed
                exporters.Add(exporter);
        }
    }

    internal string ResolveOutputPath(string savePath, string ext, string? nameSuffix = null)
    {
        var fullPath = Path.Combine(BaseDirectory.FullName, savePath) + nameSuffix + '.' + ext.ToLower();
        var dir = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException($"Cannot determine directory for path: {fullPath}");
        Directory.CreateDirectory(dir);
        return fullPath.Replace('/', '\\');
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
