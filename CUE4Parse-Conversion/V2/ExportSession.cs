using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.V2.Exporters;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;

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
        switch (export)
        {
            // ------- Exporters -------
            case UTexture texture: return Add(new TextureExporter2(texture));
            case UMaterialInterface material: return Add(new MaterialExporter3(material));
            case USkeletalMesh skeletalMesh: return Add(new SkeletalMeshExporter(skeletalMesh));
            case UStaticMesh staticMesh: return Add(new StaticMeshExporter(staticMesh));
            case USkeleton skeleton: return Add(new SkeletonExporter(skeleton));
            case UAnimationAsset animation: return Add(new AnimationExporter2(animation));
            case UDNAAsset dna: return Add(new DnaExporter(dna));
            case UWorld world: return Add(new WorldExporter(world));
            case ALandscapeProxy landscape: return Add(new LandscapeMeshExporter(landscape));
            case USplineMeshComponent spline: return Add(new SplineMeshExporter(spline));

            // ------- References -------
            case UStaticMeshComponent sm when sm.GetStaticMesh().TryLoad<UStaticMesh>(out var mesh):
                foreach (var overrideMaterial in sm.OverrideMaterials)
                {
                    if (overrideMaterial?.TryLoad<UMaterialInterface>(out var material) == true)
                    {
                        Add(material);
                    }
                }
                return Add(mesh);
            case USkeletalMeshComponent sk when sk.GetSkeletalMesh().TryLoad<USkeletalMesh>(out var mesh):
                if (sk.AnimationData?.AnimToPlay.TryLoad<UAnimationAsset>(out var animToPlay) == true)
                {
                    Add(animToPlay);
                }
                foreach (var overrideMaterial in sk.OverrideMaterials)
                {
                    if (overrideMaterial?.TryLoad<UMaterialInterface>(out var material) == true)
                    {
                        Add(material);
                    }
                }
                return Add(mesh);
            case UBillboardComponent billboard when billboard.GetSprite() is { } sprite:
                return Add(sprite);
            // case UBrushComponent:
            // case UShapeComponent:
            // case UAudioComponent:
            // case UTextRenderComponent:
            case ULandscapeComponent landscape when landscape.Outer?.TryLoad<ALandscapeProxy>(out var outer) == true:
                return Add(outer);
            case ULandscapeSplinesComponent splines:
                foreach (var ptr in splines.Segments)
                {
                    if (ptr?.TryLoad<ULandscapeSplineSegment>(out var segment) == true)
                    {
                        foreach (var meshPtr in segment.LocalMeshComponents)
                        {
                            if (meshPtr?.TryLoad<USplineMeshComponent>(out var splineMesh) == true)
                            {
                                Add(splineMesh);
                            }
                        }
                    }
                }
                return this;

            default: throw new NotSupportedException($"Could not create exporter for export of type '{export.GetType().Name}'.");
        }
    }

    public ExportSession Add(ExporterBase2 exporter)
    {
        if (!_paths.TryAdd(exporter.ObjectPath, 0)) return this;

        exporter._session = this;
        _roots.Enqueue(exporter);
        Interlocked.Increment(ref _totalQueued);
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

            await Parallel.ForEachAsync(current, options, Process).ConfigureAwait(false);
        }

        Interlocked.Exchange(ref _totalQueued, 0);
        return [.. allResults];

        async ValueTask Process(IExporter2 exporter, CancellationToken token)
        {
            var results = await exporter.ExportAsync(token).ConfigureAwait(false);

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
