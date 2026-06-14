using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.World;
using CUE4Parse_Conversion.Options;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.Exporters;

public sealed class WorldExporter(UWorld export) : ExporterBase(export)
{
    private const string Extension = "usda"; // TODO: technically only usda for now

    protected override IReadOnlyList<ExportFile> BuildExportFiles(CancellationToken ct = default)
    {
        var format = GetWorldFormat(Session.Options.MeshFormat);
        using var world = new WorldDto(export);

        var paths = new WorldAssetPaths();

        foreach (var levelWorld in world.StreamingLevels)
        {
            paths.SubLayers.Add(Resolve(levelWorld, Extension));
            Session.Add(levelWorld);
        }

        CollectFromActor(world.Actors, paths, ct);

        return [format.Build(world, paths)];
    }

    private void CollectFromActor(IEnumerable<ActorDto> actors, WorldAssetPaths paths, CancellationToken ct = default)
    {
        foreach (var actor in actors)
        {
            ct.ThrowIfCancellationRequested();
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
        }
    }

    private void CollectFromComponent(SceneComponentDto? comp, WorldAssetPaths paths)
    {
        switch (comp)
        {
            case null: return;
            case MeshComponentDto meshComp:
            {
                if (!meshComp.MeshPtr.IsNull)
                {
                    if (meshComp is SplineMeshComponentDto splineComp)
                    {
                        paths.SplineMeshes[splineComp] = Resolve(splineComp._component, Extension);
                        Session.Add(splineComp._component);
                    }
                    else if (paths.Assets.TryAdd(meshComp.MeshPtr, "") && meshComp.MeshPtr.Load<UObject>() is { } mesh)
                    {
                        paths.Assets[meshComp.MeshPtr] = Resolve(mesh, Extension);
                        if (mesh is UStaticMesh or USkeletalMesh) Session.Add(mesh);
                    }
                }

                if (meshComp.OverrideMaterials is { Length: > 0 } overrides)
                {
                    foreach (var ptr in overrides)
                    {
                        if (ptr is null || ptr.IsNull || !paths.Assets.TryAdd(ptr, "") ||
                            ptr.Load<UMaterialInterface>() is not { } material) continue;

                        paths.Assets[ptr] = Resolve(material, Extension);
                        if (Session.Options.ExportMaterials) Session.Add(material);
                    }
                }
                break;
            }
            case LandscapeMeshComponentDto landscapeComp:
            {
                paths.LandscapeMeshes[landscapeComp] = Resolve(landscapeComp._component, Extension);
                if (LandscapeMeshComponentDto.PerComponentExport)
                {
                    Session.Add(landscapeComp._component);
                }
                else if (landscapeComp.OuterProxy is { } proxy)
                {
                    Session.Add(proxy);
                }
                break;
            }
        }

        foreach (var child in comp.Children)
        {
            CollectFromComponent(child, paths);
        }
    }

    private IWorldExportFormat GetWorldFormat(EMeshFormat format) => format switch
    {
        EMeshFormat.USD => new UsdWorldFormat(),
        // EMeshFormat.UEFormat => new UEFormatWorldFormat(),
        _ => throw new NotSupportedException($"World export does not support format {format}. Available formats: {string.Join(", ", "USD")}")
    };
}
