using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CUE4Parse_Conversion.Exporters;
using CUE4Parse_Conversion.Options;
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
    public int TotalQueued => _totalQueued;

    private readonly Lock _queueLock = new();
    private readonly ConcurrentQueue<IExporter> _roots = new();
    private readonly ConcurrentDictionary<string, byte> _paths = new(StringComparer.OrdinalIgnoreCase);

    public ExportSession Add(UObject export)
    {
        return export switch
        {
            UTexture texture => Add(new TextureExporter(texture)),
            UMaterialInterface material => Add(new MaterialExporter(material)),
            USkeletalMesh skeletalMesh => Add(new SkeletalMeshExporter(skeletalMesh)),
            UStaticMesh staticMesh => Add(new StaticMeshExporter(staticMesh)),
            USkeleton skeleton => Add(new SkeletonExporter(skeleton)),
            UPoseAsset poseAsset => Add(new PoseAssetExporter(poseAsset)),
            UAnimationAsset animation => Add(new AnimationExporter(animation)),
            UDNAAsset dna => Add(new DnaExporter(dna)),
            UWorld world => Add(new WorldExporter(world)),
            ALandscapeProxy landscape => Add(new LandscapeMeshExporter(landscape)),
            ULandscapeComponent landscape => Add(new LandscapeMeshExporter2(landscape)),
            USplineMeshComponent spline => Add(new SplineMeshExporter(spline)),
            _ => throw new NotSupportedException($"Could not create exporter for export of type '{export.GetType().Name}'.")
        };
    }

    public ExportSession Add(ExporterBase exporter)
    {
        lock (_queueLock)
        {
            // TODO: this prevents 2 exporters messing with the same file from being enqueued in the same run (e.g. MeshExporter / RawDataExporter)
            if (!_paths.TryAdd(exporter.ObjectPath, 0)) return this;

            exporter._session = this;
            _roots.Enqueue(exporter);

            Interlocked.Increment(ref _totalQueued);
        }
        OnPropertyChanged(nameof(TotalQueued));
        exporter.Log.Debug("Queued for export");
        return this;
    }

    public bool Remove(string objectPath)
    {
        lock (_queueLock)
        {
            if (_baseDirectory != null || !_paths.ContainsKey(objectPath))
                return false;

            var count = _roots.Count;
            var removed = false;
            for (var i = 0; i < count && _roots.TryDequeue(out var exporter); i++)
            {
                if (!removed && exporter.ObjectPath.Equals(objectPath, StringComparison.OrdinalIgnoreCase))
                {
                    removed = true;
                    continue;
                }

                _roots.Enqueue(exporter);
            }

            if (!removed)
                return false;

            _paths.TryRemove(objectPath, out _);
            Interlocked.Decrement(ref _totalQueued);
        }

        OnPropertyChanged(nameof(TotalQueued));
        return true;
    }

    public void Clear()
    {
        lock (_queueLock)
        {
            _roots.Clear();
            _paths.Clear();
            Interlocked.Exchange(ref _totalQueued, 0);
        }
        OnPropertyChanged(nameof(TotalQueued));
    }

    public async Task<IReadOnlyList<ExportResult>> RunAsync(string baseDirectory, ExportOptions options, IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        lock (_queueLock)
        {
            _baseDirectory = new DirectoryInfo(baseDirectory);
            _options = options;
        }

        var completed = 0;
        var allResults = new ConcurrentBag<ExportResult>();

        try
        {
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = ct };
            var current = new List<IExporter>();
            while (true)
            {
                current.Clear();
                while (_roots.TryDequeue(out var exporter))
                {
                    ct.ThrowIfCancellationRequested();
                    current.Add(exporter);
                }
                if (current.Count == 0) break;

                await Parallel.ForEachAsync(current, parallelOptions, Process).ConfigureAwait(false);
            }
        }
        finally // just in case cancellation is requested, we still need to clear things up
        {
            Clear();
            progress?.Report(new ExportProgress(completed, completed + _totalQueued)); // this ensure the last progress reports the actual numbers

            _baseDirectory = null;
            _options = null;
        }

        return [.. allResults];

        async ValueTask Process(IExporter exporter, CancellationToken token)
        {
            var results = await exporter.ExportAsync(token).ConfigureAwait(false);

            Interlocked.Decrement(ref _totalQueued);
            OnPropertyChanged(nameof(TotalQueued));

            foreach (var result in results)
            {
                allResults.Add(result);

                var c = Interlocked.Increment(ref completed);
                progress?.Report(new ExportProgress(c, completed + _totalQueued, result));
            }
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
