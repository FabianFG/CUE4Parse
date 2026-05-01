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

        var ptrs = new HashSet<FPackageIndex>();
        CollectMeshRefs(world.Actors, ptrs);

        var meshes = new Dictionary<FPackageIndex, string>();
        foreach (var ptr in ptrs)
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

        var file = format.Build(world, meshes, subLayers);
        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }

    private string GetRelativeAssetPath(UObject obj)
    {
        var rawPath = obj.Owner?.Name ?? obj.GetPathName();
        var packagePath = (obj.Owner?.Provider?.FixPath(rawPath) ?? rawPath).SubstringBeforeLast('.').Replace('\\', '/');
        var worldDir = PackageDirectory.Replace('\\', '/');

        var rel = Path.GetRelativePath(worldDir.Replace('/',  Path.DirectorySeparatorChar), packagePath.Replace('/', Path.DirectorySeparatorChar)).Replace(Path.DirectorySeparatorChar, '/');
        if (!rel.StartsWith("./") && !rel.StartsWith("../"))
        {
            rel = "./" + rel;
        }

        return rel + ".usda";
    }

    private void CollectMeshRefs(IEnumerable<ActorDto> actors, HashSet<FPackageIndex> refs)
    {
        foreach (var actor in actors)
        {
            CollectFromComponent(actor.RootComponent, refs);
            CollectMeshRefs(actor.ChildActors, refs);
        }
    }

    private void CollectFromComponent(SceneComponentDto? comp, HashSet<FPackageIndex> refs)
    {
        if (comp is null) return;

        switch (comp)
        {
            case StaticMeshComponentDto { StaticMesh.IsNull: false } sm:
                refs.Add(sm.StaticMesh);
                break;
            case SkinnedMeshComponentDto { SkinnedMesh.IsNull: false } sk:
                refs.Add(sk.SkinnedMesh);
                break;
        }

        foreach (var child in comp.Children)
        {
            CollectFromComponent(child, refs);
        }
    }

    private IWorldExportFormat GetWorldFormat(EWorldFormat fmt) => fmt switch
    {
        EWorldFormat.USD => new UsdWorldFormat(),
        EWorldFormat.UEFormat => new UEFormatWorldFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(fmt), fmt, "Unsupported world format")
    };
}
