using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse_Conversion.Dto.Custom.DBD;

public sealed class DeadByDaylightMapDto : SceneDto
{
    public DeadByDaylightMapDto(UPaperTileMap asset, int seed = 1, CancellationToken ct = default) : base(asset)
    {
        var map = new FProceduralLevelLayout(asset, seed, ct);
        foreach (var placement in map.Placements)
        {
            ct.ThrowIfCancellationRequested();
            Actors.Add(new PlacedTileActor(placement, placement.Transform(map.TileSize, map.Width, map.Height), ct));
        }

        foreach (var quadrant in map.Quadrants)
        {
            ct.ThrowIfCancellationRequested();
            Actors.Add(new PlacedBlueprintActor(quadrant.Name, quadrant.Blueprint, quadrant.Transform, ct, quadrant.SpawnerActivation));
        }

        foreach (var placement in map.BasementActors.Concat(map.GameplayActors))
        {
            ct.ThrowIfCancellationRequested();
            Actors.Add(new PlacedBlueprintActor(placement.Name, placement.Blueprint, placement.Transform, ct));
        }

        if (map.LightingBlueprint is { } lighting)
        {
            ct.ThrowIfCancellationRequested();
            Actors.Add(new PlacedBlueprintActor("Environment", lighting, map.EnvironmentTransform, ct));
        }

        // Remove Special Event decorations, I don't want these obviously
        foreach (var actor in Actors)
        {
            if (actor.RootComponent is { } root)
            {
                RemoveEventDecorations(root);
            }
        }
    }

    private static void RemoveEventDecorations(SceneComponentDto component)
    {
        component.Children.RemoveAll(child =>
        {
            if (IsEventMesh(child))
            {
                child.Dispose();
                return true;
            }
            RemoveEventDecorations(child);
            return false;
        });

        component.AttachedActors.RemoveAll(actor =>
        {
            if (FActorSpawnerVisualization.IsEventAsset(actor.Path) || IsEventMesh(actor.RootComponent))
            {
                actor.Dispose();
                return true;
            }
            if (actor.RootComponent is { } root)
                RemoveEventDecorations(root);
            return false;
        });

        static bool IsEventMesh(SceneComponentDto? child) => child is MeshComponentDto mesh && FActorSpawnerVisualization.IsEventAsset(mesh.MeshPtr.ResolvedObject?.GetPathName());
    }

    private sealed class PlacedBlueprintActor : ActorDto
    {
        public PlacedBlueprintActor(string name, UBlueprintGeneratedClass blueprint, FTransform transform, CancellationToken ct, IReadOnlyDictionary<string, bool>? activation = null) : base(blueprint)
        {
            ct.ThrowIfCancellationRequested();

            Name = name;
            RootComponent = new SceneComponentDto(transform, "Placement", this);
            var pose = FProceduralComponentReader.ReadPreviewTransforms(blueprint);
            var componentNames = activation == null && pose.Count == 0 ? null : FProceduralComponentReader.ReadNamedComponents<USceneComponent>(blueprint)
                .ToDictionary(entry => entry.Component.Name, entry => entry.Name, StringComparer.Ordinal);
            var actor = new BlueprintActorDto(blueprint);
            if (actor.RootComponent != null)
            {
                actor.RootComponent.Transform = FTransform.Identity;
                if (componentNames != null)
                    ApplyComponents(actor.RootComponent);
                RootComponent.AttachedActors.Add(actor);
            }
            else
            {
                actor.Dispose();
            }

            void ApplyComponents(SceneComponentDto component)
            {
                foreach (var child in component.Children)
                    ApplyComponents(child);

                var componentName = componentNames?.GetValueOrDefault(component.Name, component.Name) ?? component.Name;
                if (componentName.EndsWith(USimpleConstructionScript.ComponentTemplateNameSuffix, StringComparison.Ordinal))
                    componentName = componentName[..^USimpleConstructionScript.ComponentTemplateNameSuffix.Length];
                if (pose.TryGetValue(componentName, out var posedTransform))
                    component.Transform = posedTransform;
                if (activation?.GetValueOrDefault(componentName, true) != false)
                    return;

                foreach (var child in component.AttachedActors)
                {
                    child.Dispose();
                }

                component.AttachedActors.Clear();
            }
        }
    }

    private sealed class PlacedTileActor : ActorDto
    {
        public PlacedTileActor(FProceduralTilePlacement placement, FTransform transform, CancellationToken ct) : base(placement.Tile.Blueprint)
        {
            ct.ThrowIfCancellationRequested();
            Name = $"Tile_{placement.X}_{placement.Y}_{placement.Tile.Blueprint.Name}";
            RootComponent = new SceneComponentDto(transform, "Placement", this);
            var actor = new BlueprintActorDto(placement.Tile.Blueprint);
            if (actor.RootComponent == null)
            {
                actor.Dispose();
                throw new InvalidDataException($"Tile Blueprint {placement.Tile.AssetPath} has no scene components.");
            }

            actor.RootComponent.Transform = FTransform.Identity;

            IReadOnlyList<(string Name, UActorSpawner Component, FTransform Transform)>? spawners = null;
            var componentNames = FProceduralComponentReader.ReadNamedComponents<USceneComponent>(placement.Tile.Blueprint)
                .ToDictionary(entry => entry.Component.Name, entry => entry.Name, StringComparer.Ordinal);
            if (placement.SpawnerActivation is { } activation)
                ApplySpawnerActivation(actor.RootComponent);
            if (placement.ComponentVariations is { } variations)
                ApplyComponentVariations(actor.RootComponent);

            RootComponent.AttachedActors.Add(actor);

            void ApplyComponentVariations(SceneComponentDto component)
            {
                foreach (var child in component.Children)
                    ApplyComponentVariations(child);

                var name = componentNames.GetValueOrDefault(component.Name, component.Name);
                if (name.EndsWith(USimpleConstructionScript.ComponentTemplateNameSuffix, StringComparison.Ordinal))
                    name = name[..^USimpleConstructionScript.ComponentTemplateNameSuffix.Length];
                if (!variations.TryGetValue(name, out var blueprint))
                    return;

                foreach (var child in component.AttachedActors)
                {
                    child.Dispose();
                }

                component.AttachedActors.Clear();

                var variant = new BlueprintActorDto(blueprint, [placement.Tile.Blueprint.GetPathName()], 1);
                if (variant.RootComponent is { } variantRoot)
                {
                    variantRoot.Transform = FTransform.Identity;
                    component.AttachedActors.Add(variant);
                }
                else
                {
                    variant.Dispose();
                }
            }

            void ApplySpawnerActivation(SceneComponentDto component)
            {
                foreach (var child in component.Children)
                {
                    ApplySpawnerActivation(child);
                }

                var name = componentNames.GetValueOrDefault(component.Name, component.Name);
                if (name.EndsWith(USimpleConstructionScript.ComponentTemplateNameSuffix, StringComparison.Ordinal))
                    name = name[..^USimpleConstructionScript.ComponentTemplateNameSuffix.Length];

                if (!activation.TryGetValue(name, out var active))
                    return;

                if (!active)
                {
                    foreach (var child in component.AttachedActors)
                    {
                        child.Dispose();
                    }

                    component.AttachedActors.Clear();
                }
                else if (component.AttachedActors.Count == 0)
                {
                    spawners ??= FProceduralComponentReader.ReadNamedComponents<UActorSpawner>(placement.Tile.Blueprint);
                    var spawner = spawners.FirstOrDefault(entry => entry.Name == name).Component;
                    if (spawner == null || FActorSpawnerVisualization.GetBlueprint(spawner, true) is not { } blueprint || blueprint.GetPathName() == placement.Tile.Blueprint.GetPathName())
                        return;

                    var child = new BlueprintActorDto(blueprint, [placement.Tile.Blueprint.GetPathName()], 1);
                    if (child.RootComponent is { } childRoot)
                    {
                        childRoot.Transform = FTransform.Identity;
                        component.AttachedActors.Add(child);
                    }
                    else
                    {
                        child.Dispose();
                    }
                }
            }
        }
    }
}
