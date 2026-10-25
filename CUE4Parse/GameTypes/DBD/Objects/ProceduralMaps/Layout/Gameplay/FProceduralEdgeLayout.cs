using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal static class FProceduralEdgeLayout
{
    public static List<FProceduralActorPlacement> Build(IEnumerable<FProceduralActorPlacement> placements, IEnumerable<FProceduralSpawnPoint> points, int seed, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var candidates = points.Where(point => point.IsActive && point.Component is UActorSpawner { Type: EGameplayElementType.EdgeObjects, Weight: > 0, SpawnCountMax: not 0 }).ToArray();
        if (candidates.Length == 0)
            return [];

        var hosts = placements.ToArray();
        var geometryByClass = new Dictionary<UBlueprintGeneratedClass, FBox[]>();
        var shapesByMesh = new Dictionary<FPackageIndex, FBox[]>();
        var ancestry = new HashSet<UBlueprintGeneratedClass>();
        var blockers = hosts.Select(host => (host.Name, Bounds: GetGeometry(host.Blueprint).Select(box => box.TransformBy(host.Transform.ToMatrixWithScale())).ToArray())).ToList();
        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.EdgeObjects);
        // The native edge pass shuffles against the full candidate array before its priority sort
        for (var index = candidates.Length - 1; index >= 0; index--)
        {
            var other = (int) (random.NextFloat() * candidates.Length);
            (candidates[index], candidates[other]) = (candidates[other], candidates[index]);
        }

        var result = new List<FProceduralActorPlacement>();
        foreach (var point in candidates.OrderByDescending(point => point.Priority))
        {
            ct.ThrowIfCancellationRequested();
            var spawner = (UActorSpawner) point.Component;
            var selection = new FProceduralRandomStream(unchecked((int) random.NextUInt32() + 1), EProceduralRandomStream.Default);
            var path = FProceduralGameplayLayout.SelectBlueprint(point.Elements ?? spawner.ActivatedSceneElement, point.Elements == null ? spawner.Visualization : default, ref selection);
            if (!path.TryLoad<UBlueprintGeneratedClass>(out var blueprint))
                continue;

            var bounds = GetGeometry(blueprint).Select(box => box.TransformBy(point.Transform.ToMatrixWithScale())).ToArray();
            var host = hosts.FirstOrDefault(host => point.Name.StartsWith(host.Name + '_', StringComparison.Ordinal));
            if (blockers.Any(blocker => blocker.Name != host?.Name && bounds.Any(box => blocker.Bounds.Any(other => Overlaps(box, other)))))
                continue;

            result.Add(new FProceduralActorPlacement(point.Name, blueprint, point.Transform));
            blockers.Add((host?.Name ?? point.Name, bounds));
        }
        return result;

        FBox[] GetGeometry(UBlueprintGeneratedClass blueprint)
        {
            ct.ThrowIfCancellationRequested();
            if (geometryByClass.TryGetValue(blueprint, out var cached))
                return cached;
            if (ancestry.Count >= 8 || !ancestry.Add(blueprint))
                return [];

            var boxes = new List<FBox>();
            foreach (var (component, transform) in FProceduralComponentReader.ReadComponents<USceneComponent>(blueprint))
            {
                ct.ThrowIfCancellationRequested();
                if (component is UStaticMeshComponent mesh && component.GetOrDefault("bVisible", true) && !component.GetOrDefault("bHiddenInGame", false))
                {
                    var body = component.GetOrDefault<FStructFallback?>("BodyInstance");
                    if (body?.GetOrDefault<FName>("CollisionProfileName").Text == "NoCollision" || body?.GetOrDefault("CollisionEnabled", ECollisionEnabled.QueryAndPhysics) is ECollisionEnabled.NoCollision)
                        continue;

                    var meshPtr = mesh.GetStaticMesh();
                    if (!shapesByMesh.TryGetValue(meshPtr, out var shapes))
                    {
                        var geometry = ReadCollisionGeometry(meshPtr);
                        shapes = geometry == null ? [] :
                        [
                            .. geometry.ConvexElems.Select(elem => elem.ElemBox.TransformBy(elem.Transform.ToMatrixWithScale())),
                            .. geometry.BoxElems.Select(elem => new FBox(new FVector(-elem.X, -elem.Y, -elem.Z) / 2, new FVector(elem.X, elem.Y, elem.Z) / 2)
                                    .TransformBy(new FTransform(elem.Rotation, elem.Center, FVector.OneVector).ToMatrixWithScale())),
                        ];
                        shapesByMesh.Add(meshPtr, shapes);
                    }

                    if (mesh is UInstancedStaticMeshComponent instanced)
                    {
                        foreach (var instance in instanced.PerInstanceSMData ?? [])
                        {
                            boxes.AddRange(shapes.Select(box => box.TransformBy((instance.TransformData * transform).ToMatrixWithScale())));
                        }
                    }
                    else
                    {
                        boxes.AddRange(shapes.Select(box => box.TransformBy(transform.ToMatrixWithScale())));
                    }
                }

                var child = component is UChildActorComponent
                    ? component.GetOrDefault<FPackageIndex?>("ChildActorClass")?.Load<UBlueprintGeneratedClass>()
                    : FActorSpawnerVisualization.GetBlueprint(component);

                if (child != null)
                    boxes.AddRange(GetGeometry(child).Select(box => box.TransformBy(transform.ToMatrixWithScale())));
            }

            ancestry.Remove(blueprint);
            return geometryByClass[blueprint] = [.. boxes];
        }
    }

    private static FKAggregateGeom? ReadCollisionGeometry(FPackageIndex mesh)
    {
        if (mesh.ResolvedObject is not { } resolved)
            return null;

        // Read the mesh's body setup without loading its render buffers
        var package = resolved.Package;
        for (var index = 0; index < package.ExportMapLength; index++)
        {
            var body = package.ResolvePackageIndex(new FPackageIndex(package, index + 1));
            if (body?.Class?.Name.Text == "BodySetup" && body.Outer?.ExportIndex == resolved.ExportIndex)
                return (body.Object?.Value as UBodySetup)?.AggGeom;
        }

        return null;
    }

    internal static bool Overlaps(FBox first, FBox second) =>
        MathF.Min(first.Max.X, second.Max.X) - MathF.Max(first.Min.X, second.Min.X) > UnrealMath.KindaSmallNumber &&
        MathF.Min(first.Max.Y, second.Max.Y) - MathF.Max(first.Min.Y, second.Min.Y) > UnrealMath.KindaSmallNumber &&
        MathF.Min(first.Max.Z, second.Max.Z) - MathF.Max(first.Min.Z, second.Min.Z) > UnrealMath.KindaSmallNumber;
}
