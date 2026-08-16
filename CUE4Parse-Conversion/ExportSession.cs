using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse_Conversion.Exporters;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion;

public sealed class ExportSession(Action<StreamingLevelFilterArgs, CancellationToken>? streamingLevelFilter = null) : INotifyPropertyChanged
{
    internal readonly Action<StreamingLevelFilterArgs, CancellationToken>? _streamingLevelFilter = streamingLevelFilter;
    internal readonly SemaphoreSlim _streamingLevelFilterLock = new(1, 1);

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    private DirectoryInfo? _baseDirectory;
    internal DirectoryInfo BaseDirectory => _baseDirectory ?? throw new InvalidOperationException("Session is not currently running.");

    private ExportOptions? _options;
    internal ExportOptions Options => _options ?? throw new InvalidOperationException("Session is not currently running.");

    private int _totalQueued;
    public int TotalQueued => Volatile.Read(ref _totalQueued);
    public bool HasQueuedItems => TotalQueued > 0;

    private int _running;
    public bool IsRunning => Volatile.Read(ref _running) == 1;

    private readonly Channel<QueueEntry> _queue = Channel.CreateUnbounded<QueueEntry>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
    private readonly ConcurrentDictionary<string, QueueEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Queues a built-in exporter for an Unreal object. Provider-backed package exports are held weakly and
    /// reloaded if necessary, allowing their package data to be collected while the session is waiting to run.
    /// Use <see cref="Add(ExporterBase)"/> when the exact in-memory object must remain alive.
    /// </summary>
    public ExportSession Add(UObject export)
    {
        var exporter = CreateExporter(export);
        return Add(DeferredObjectExporter.TryCreate(export) ?? exporter);
    }

    internal static ExporterBase CreateExporter(UObject export)
    {
        return export switch
        {
            UTexture texture => new TextureExporter(texture),
            UMaterialInterface material => new MaterialExporter(material),
            USkeletalMesh skeletalMesh => new SkeletalMeshExporter(skeletalMesh),
            UStaticMesh staticMesh => new StaticMeshExporter(staticMesh),
            USkeleton skeleton => new SkeletonExporter(skeleton),
            UPoseAsset poseAsset => new PoseAssetExporter(poseAsset),
            UAnimationAsset animation => new AnimationExporter(animation),
            UDNAAsset dna => new DnaExporter(dna),
            UWorld world => new WorldExporter(world),
            ALandscapeProxy landscape => new LandscapeMeshExporter(landscape),
            ULandscapeComponent landscape => new LandscapeMeshExporter2(landscape),
            USplineMeshComponent spline => new SplineMeshExporter(spline),
            _ => throw new NotSupportedException($"Could not create exporter for export of type '{export.GetType().Name}'.")
        };
    }

    /// <summary>
    /// Queues a preconfigured exporter. The exporter is strongly referenced until it is processed, removed,
    /// or the session is cleared.
    /// </summary>
    public ExportSession Add(ExporterBase exporter)
    {
        // TODO: this prevents 2 exporters messing with the same file from being enqueued in the same run (e.g. MeshExporter / RawDataExporter)
        var entry = new QueueEntry(exporter);
        if (!_entries.TryAdd(exporter.ObjectPath, entry)) return this;

        exporter._session = this;
        Interlocked.Increment(ref _totalQueued);
        if (!_queue.Writer.TryWrite(entry))
        {
            _entries.TryRemove(exporter.ObjectPath, out _);
            Interlocked.Decrement(ref _totalQueued);
            throw new InvalidOperationException("The export queue is not accepting new items.");
        }

        OnPropertyChanged(nameof(TotalQueued));
        OnPropertyChanged(nameof(HasQueuedItems));
        exporter.Log.Debug("Queued for export");
        return this;
    }

    public bool Remove(string objectPath)
    {
        if (IsRunning || !_entries.TryRemove(objectPath, out var entry))
            return false;

        entry.Take();
        Interlocked.Decrement(ref _totalQueued);
        OnPropertyChanged(nameof(TotalQueued));
        OnPropertyChanged(nameof(HasQueuedItems));
        return true;
    }

    public void Clear()
    {
        while (_queue.Reader.TryRead(out _)) { }
        _entries.Clear();
        Interlocked.Exchange(ref _totalQueued, 0);
        OnPropertyChanged(nameof(TotalQueued));
        OnPropertyChanged(nameof(HasQueuedItems));
    }

    public async Task<IReadOnlyList<ExportResult>> RunAsync(string baseDirectory, ExportOptions options, IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
            throw new InvalidOperationException("Session is already running.");

        OnPropertyChanged(nameof(IsRunning));
        _baseDirectory = new DirectoryInfo(baseDirectory);
        _options = options;

        var results = new ConcurrentQueue<ExportResult>();
        try
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = ct
            };
            var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (TotalQueued == 0) drained.TrySetResult();

            await Parallel.ForEachAsync(ReadAll(), parallelOptions, Process).ConfigureAwait(false);

            async IAsyncEnumerable<ExporterBase> ReadAll([EnumeratorCancellation] CancellationToken token = default)
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (_queue.Reader.TryRead(out var entry))
                    {
                        if (entry.Take() is { } exporter)
                            yield return exporter;
                        continue;
                    }

                    if (TotalQueued == 0) yield break;

                    var workAvailable = _queue.Reader.WaitToReadAsync(token).AsTask();
                    if (await Task.WhenAny(workAvailable, drained.Task).ConfigureAwait(false) == drained.Task)
                        yield break;

                    if (!await workAvailable.ConfigureAwait(false)) yield break;
                }
            }

            async ValueTask Process(ExporterBase exporter, CancellationToken token)
            {
                var result = await exporter.ExportAsync(token).ConfigureAwait(false);
                results.Enqueue(result);

                var stillQueued = Interlocked.Decrement(ref _totalQueued);
                var count = results.Count;
                OnPropertyChanged(nameof(TotalQueued));
                OnPropertyChanged(nameof(HasQueuedItems));

                progress?.Report(new ExportProgress(count, count + stillQueued, result));
                if (stillQueued == 0) drained.TrySetResult();
            }
        }
        finally // just in case cancellation is requested, we still need to clear things up
        {
            var stillQueued = TotalQueued;
            var count = results.Count;

            Clear();
            progress?.Report(new ExportProgress(count, count + stillQueued)); // this ensure the last progress reports the actual numbers

            _options = null;
            _baseDirectory = null;
            Interlocked.Exchange(ref _running, 0);
            OnPropertyChanged(nameof(IsRunning));
        }

        return [.. results];
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

    private sealed class QueueEntry(ExporterBase exporter)
    {
        private ExporterBase? _exporter = exporter;
        public ExporterBase? Take() => Interlocked.Exchange(ref _exporter, null);
    }
}
