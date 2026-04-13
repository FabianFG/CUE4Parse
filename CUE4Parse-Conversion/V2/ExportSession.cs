using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.V2.Exporters;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace CUE4Parse_Conversion.V2;

public sealed class ExportSession(DirectoryInfo baseDirectory, ExporterOptions options)
{
    public DirectoryInfo BaseDirectory { get; } = baseDirectory;
    public ExporterOptions Options { get; } = options;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    private readonly ConcurrentQueue<IExporter2> _roots = new();
    private readonly ConcurrentDictionary<string, byte> _paths = new(StringComparer.OrdinalIgnoreCase);
    private int _totalQueued;

    public ExportSession(string baseDirectory, ExporterOptions options) : this(new DirectoryInfo(baseDirectory), options)
    {

    }

    public ExportSession Add(UObject export)
    {
        return export switch
        {
            UTexture texture => Add(new TextureExporter2(texture)),
            UMaterialInterface material => Add(new MaterialExporter3(material)),
            USkeletalMesh skeletalMesh => Add(new MeshExporter2(skeletalMesh)),
            UStaticMesh staticMesh => Add(new MeshExporter2(staticMesh)),
            USkeleton skeleton => Add(new MeshExporter2(skeleton)),
            UAnimationAsset animation => Add(new AnimationExporter2(animation)),
            // TODO: landscape
            // TODO: world
            // TODO: components?
            _ => throw new NotSupportedException($"Could not create exporter for export of type '{export.GetType().Name}'.")
        };
    }

    public ExportSession Add(ExporterBase2 exporter)
    {
        if (!_paths.TryAdd(exporter.ObjectPath, 0)) return this;

        exporter._session = this;
        _roots.Enqueue(exporter);
        return this;
    }

    public ExportSession AddRange(IEnumerable<ExporterBase2> exporters)
    {
        foreach (var exporter in exporters)
        {
            Add(exporter);
        }
        return this;
    }

    public async Task<IReadOnlyList<ExportResult>> RunAsync(IProgress<ExportProgress>? progress = null, CancellationToken ct = default)
    {
        var completed = 0;
        var allResults = new ConcurrentBag<ExportResult>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = ct };

        var current = new List<IExporter2>();
        while (true)
        {
            current.Clear();
            while (_roots.TryDequeue(out var exporter))
            {
                ct.ThrowIfCancellationRequested();
                current.Add(exporter);
            }
            if (current.Count == 0) break;

            Interlocked.Add(ref _totalQueued, current.Count);
            await Parallel.ForEachAsync(current, options, Process).ConfigureAwait(false);
        }

        Interlocked.Exchange(ref _totalQueued, 0);
        return [.. allResults];

        async ValueTask Process(IExporter2 exporter, CancellationToken token)
        {
            IReadOnlyList<ExportResult> results;
            try
            {
                results = await exporter.ExportAsync(progress, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results = [ExportResult.Failure(exporter.ObjectPath, ex)];
            }

            foreach (var result in results)
            {
                allResults.Add(result);

                var c = Interlocked.Increment(ref completed);
                var total = Volatile.Read(ref _totalQueued);
                progress?.Report(new ExportProgress(c, total, result));
            }
        }
    }
}
