using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal sealed record FProceduralSpawnPoint(string Name, USceneComponent Component, FTransform Transform)
{
    public bool IsActive { get; init; } = Component is not UActorSpawner { ActivatedByDefault: false };
    public FActorSpawnerProperties[]? Elements { get; init; }
    public double Weight => Component switch
    {
        UActorSpawner spawner => spawner.Weight,
        UTileSpawnPoint point => point.Weight,
        _ => 0
    };
    public int Priority => Component switch
    {
        UActorSpawner spawner => spawner.SpawnPriorityTier,
        UTileSpawnPoint point => point._spawnPriorityTier,
        _ => 0
    };
    public float Cost => Component is UActorSpawner spawner ? spawner.Cost : 1;
    public bool WeightInfluenceable => Component is not (UActorSpawner { WeightInfluenceable: false } or UTileSpawnPoint { _weightInfluenceable: false });
    public bool WeightInfluencer => Component is not (UActorSpawner { WeightInfluencer: false } or UTileSpawnPoint { _weightInfluencer: false });

    public static List<FProceduralSpawnPoint> Collect(IEnumerable<FProceduralActorPlacement> placements, CancellationToken ct)
    {
        var result = new List<FProceduralSpawnPoint>();
        var componentsByClass = new Dictionary<string, IReadOnlyList<(string Name, USceneComponent Component, FTransform Transform)>>(StringComparer.Ordinal);
        var ancestry = new HashSet<string>(StringComparer.Ordinal);
        foreach (var placement in placements)
        {
            Visit(placement.Blueprint, placement.Name, placement.Transform, placement.SpawnerActivation, placement.ComponentVariations, placement.SpawnerElements);
        }

        return result;

        void Visit(UBlueprintGeneratedClass blueprint, string name, FTransform parentTransform,
            IReadOnlyDictionary<string, bool>? activation = null, IReadOnlyDictionary<string, UBlueprintGeneratedClass>? variations = null,
            IReadOnlyDictionary<string, FActorSpawnerProperties[]>? elements = null)
        {
            ct.ThrowIfCancellationRequested();
            var classPath = blueprint.GetPathName();
            if (ancestry.Count >= 8 || !ancestry.Add(classPath))
                return;

            try
            {
                if (!componentsByClass.TryGetValue(classPath, out var components))
                    componentsByClass.Add(classPath, components = FProceduralComponentReader.ReadNamedComponents<USceneComponent>(blueprint));

                foreach (var (variableName, component, relativeTransform) in components)
                {
                    if (activation?.GetValueOrDefault(variableName, true) == false)
                        continue;

                    bool? active = activation?.TryGetValue(variableName, out var value) == true ? value : null;
                    var transform = relativeTransform * parentTransform;
                    var componentName = $"{name}_{component.Name}";
                    if (component is UActorSpawner or UTileSpawnPoint)
                        result.Add(new FProceduralSpawnPoint(componentName, component, transform)
                        {
                            IsActive = active ?? component is not UActorSpawner { ActivatedByDefault: false },
                            Elements = elements?.GetValueOrDefault(variableName)
                        });

                    if (variations?.TryGetValue(variableName, out UBlueprintGeneratedClass? childClass) != true)
                    {
                        childClass = component is UChildActorComponent
                            ? component.GetOrDefault<FPackageIndex>("ChildActorClass")?.Load<UBlueprintGeneratedClass>()
                            : FActorSpawnerVisualization.GetBlueprint(component, active);
                    }

                    if (childClass != null)
                        Visit(childClass, componentName, transform);
                }
            }
            finally
            {
                ancestry.Remove(classPath);
            }
        }
    }
}
