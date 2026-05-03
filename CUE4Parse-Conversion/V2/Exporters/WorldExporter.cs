using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.V2.Dto.World;
using CUE4Parse_Conversion.V2.Formats.World;
using CUE4Parse_Conversion.World;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.V2.Exporters;

public sealed class WorldExporter(UWorld export) : ExporterBase2(export)
{
    private const string Extension = "usda"; // TODO: technically only usda for now

    protected override async Task<IReadOnlyList<ExportResult>> DoExportAsync(CancellationToken ct = default)
    {
        var format = GetWorldFormat(EWorldFormat.USD);
        using var world = new WorldDto(export);

        var paths = new WorldAssetPaths();

        foreach (var levelWorld in world.StreamingLevels)
        {
            paths.SubLayers.Add(Resolve(levelWorld, Extension));
            Session.Add(levelWorld);
        }

        CollectFromActor(world.Actors, paths);

        var file = format.Build(world, paths);
        var result = await WriteExportFileAsync(file, ct).ConfigureAwait(false);
        return [result];
    }

    private void CollectFromActor(IEnumerable<ActorDto> actors, WorldAssetPaths paths)
    {
        foreach (var actor in actors)
        {
            if (actor.AdditionalWorlds is { Count: > 0 })
            {
                foreach (var w in actor.AdditionalWorlds)
                {
                    if (paths.Worlds.TryAdd(w.Name, Resolve(w, Extension)))
                    {
                        Session.Add(w);
                    }
                }
            }

            CollectFromComponent(actor.RootComponent, paths);
            CollectFromActor(actor.ChildActors, paths);
        }
    }

    private void CollectFromComponent(SceneComponentDto? comp, WorldAssetPaths paths)
    {
        switch (comp)
        {
            case null: return;
            case MeshComponentDto meshComp:
                if (!meshComp.MeshPtr.IsNull && paths.Meshes.TryAdd(meshComp.MeshPtr, "") && meshComp.MeshPtr.Load<UObject>() is { } mesh)
                {
                    paths.Meshes[meshComp.MeshPtr] = Resolve(mesh, Extension);
                    if (mesh is UStaticMesh or USkeletalMesh) Session.Add(mesh);
                }

                if (meshComp.OverrideMaterials is { Length: > 0 } overrides)
                {
                    foreach (var ptr in overrides)
                    {
                        if (ptr is null || ptr.IsNull || !paths.Materials.TryAdd(ptr, "") ||
                            ptr.Load<UMaterialInterface>() is not { } material) continue;

                        if (Session.Options.ExportMaterials)
                        {
                            Session.Add(material);
                        }
                        paths.Materials[ptr] = Resolve(material, Extension);

                    }
                }
                break;
            case LandscapeMeshComponentDto landscape:
                if (LandscapeMeshComponentDto.PerComponentExport)
                {
                    Session.Add(landscape.Component);
                }
                else if (landscape.OuterProxy is { } proxy)
                {
                    Session.Add(proxy);
                }
                break;
        }

        foreach (var child in comp.Children)
        {
            CollectFromComponent(child, paths);
        }
    }

    private IWorldExportFormat GetWorldFormat(EWorldFormat format) => format switch
    {
        EWorldFormat.USD => new UsdWorldFormat(),
        EWorldFormat.UEFormat => new UEFormatWorldFormat(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported world format")
    };
}
