using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.V2.Dto.World;
using CUE4Parse_Conversion.V2.Formats.World;
using CUE4Parse_Conversion.World;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class WorldExporter(UWorld export) : ExporterBase2(export)
{
    protected override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        var format = GetWorldFormat(EWorldFormat.USD);
        var world = new WorldDto(export);

        var subLayers = new List<string>();
        foreach (var levelWorld in world.StreamingLevels)
        {
            subLayers.Add(GetRelativeAssetPath(levelWorld));
            Session.Add(levelWorld);
        }

        var worlds = new Dictionary<string, string>();
        var meshRefs = new HashSet<FPackageIndex>();
        CollectFromActor(world.Actors, meshRefs, worlds);

        var meshes = new Dictionary<FPackageIndex, string>();
        foreach (var ptr in meshRefs)
        {
            var obj = ptr.Load<UObject>();
            if (obj is null) continue;

            meshes[ptr] = GetRelativeAssetPath(obj);

            switch (obj)
            {
                case UStaticMesh:
                case USkeletalMesh:
                    Session.Add(obj);
                    break;
            }
        }

        var file = format.Build(world, meshes, subLayers, worlds.Count > 0 ? worlds : null);
        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }

    private string GetRelativeAssetPath(UObject obj)
    {
        var rawPath = obj.Owner?.Name ?? obj.GetPathName();
        var packagePath = (obj.Owner?.Provider?.FixPath(rawPath) ?? rawPath).SubstringBeforeLast('.');

        // Mirror GetSavePath: append ObjectName when the leaf differs (e.g. sub-object exports)
        if (!packagePath.SubstringAfterLast('/').Equals(obj.Name, StringComparison.OrdinalIgnoreCase))
        {
            packagePath += '/' + obj.Name;
        }

        var sep = Path.DirectorySeparatorChar;
        var rel = Path.GetRelativePath(
            PackageDirectory.Replace('/', sep),
            packagePath.Replace('/', sep)
        ).Replace(sep, '/');

        return (rel.StartsWith("./") || rel.StartsWith("../") ? rel : "./" + rel) + ".usda";
    }

    private void CollectFromActor(IEnumerable<ActorDto> actors, HashSet<FPackageIndex> meshRefs, Dictionary<string, string> worlds)
    {
        foreach (var actor in actors)
        {
            if (actor.AdditionalWorlds is { Count: > 0 })
            {
                foreach (var w in actor.AdditionalWorlds)
                {
                    if (worlds.TryAdd(w.Name, GetRelativeAssetPath(w)))
                    {
                        Session.Add(w);
                    }
                }
            }

            CollectFromComponent(actor.RootComponent, meshRefs);
            CollectFromActor(actor.ChildActors, meshRefs, worlds);
        }
    }

    private void CollectFromComponent(SceneComponentDto? comp, HashSet<FPackageIndex> meshRefs)
    {
        switch (comp)
        {
            case null: return;
            case MeshComponentDto { MeshPtr: { IsNull: false } mesh }:
                meshRefs.Add(mesh);
                break;
        }

        foreach (var child in comp.Children)
        {
            CollectFromComponent(child, meshRefs);
        }
    }

    private IWorldExportFormat GetWorldFormat(EWorldFormat format) => format switch
    {
        EWorldFormat.USD => new UsdWorldFormat(),
        EWorldFormat.UEFormat => new UEFormatWorldFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported world format")
    };
}
