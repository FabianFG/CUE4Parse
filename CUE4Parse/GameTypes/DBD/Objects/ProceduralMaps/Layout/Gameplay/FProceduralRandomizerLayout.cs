using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal static class FProceduralRandomizerLayout
{
    public static List<FProceduralActorPlacement> Build(IEnumerable<FProceduralActorPlacement> hosts, int seed, CancellationToken ct)
    {
        var result = new List<FProceduralActorPlacement>();
        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.ActorSelection);
        foreach (var host in hosts)
        {
            ct.ThrowIfCancellationRequested();
            var bindings = FProceduralRandomizerReader.Read(host.Blueprint, random);
            if (bindings.Count == 0)
                continue;

            var spawners = FProceduralComponentReader.ReadNamedComponents<UActorSpawner>(host.Blueprint).ToDictionary(entry => entry.Name, StringComparer.Ordinal);
            var activation = host.SpawnerActivation?.ToDictionary() ?? new Dictionary<string, bool>(StringComparer.Ordinal);
            var overrides = host.SpawnerElements?.ToDictionary() ?? new Dictionary<string, FActorSpawnerProperties[]>(StringComparer.Ordinal);
            var contents = new Dictionary<string, (FProceduralSpawnPoint Point, FActorSpawnerProperties[] Elements)>(StringComparer.Ordinal);
            foreach (var (name, randomizer, _) in FProceduralComponentReader.ReadNamedComponents<UObjectRandomizer>(host.Blueprint))
            {
                if (!bindings.TryGetValue(name, out var names) || randomizer.MaxPopulation <= 0 || randomizer.MaxPopulation < randomizer.MinPopulation)
                    continue;

                var points = names.Where(spawners.ContainsKey).Select(name => new FProceduralSpawnPoint(name, spawners[name].Component, spawners[name].Transform * host.Transform)).ToArray();
                var minimum = Math.Clamp(randomizer.MinPopulation, 0, points.Length);
                var maximum = Math.Clamp(randomizer.MaxPopulation, minimum, points.Length);
                var count = minimum + (int) (random.NextFloat() * (maximum - minimum + 1));
                var selected = FProceduralPopulation.SelectCount(points, count, 0, ref random, ct).ToHashSet();
                var activeIndex = 0;
                var inactiveIndex = 0;
                foreach (var point in points)
                {
                    ct.ThrowIfCancellationRequested();
                    var bIsActive = selected.Contains(point);
                    var spawner = (UActorSpawner) point.Component;

                    FActorSpawnerProperties[]? elements = null;
                    if (randomizer is UActorSpawnerRandomizer actorRandomizer)
                        elements = bIsActive ? GetElements(actorRandomizer.ActivatedSpawnerContentLists, activeIndex++) : GetElements(actorRandomizer.DeactivatedSpawnerContentLists, inactiveIndex++);

                    if (spawner.Type is not EGameplayElementType.Generic)
                    {
                        // Gameplay populations spawn these objects after randomizers set their activation and content
                        activation[point.Name] = bIsActive;
                        if (elements != null)
                            overrides[point.Name] = elements;
                        continue;
                    }

                    activation[point.Name] = false;
                    elements ??= bIsActive ? spawner.ActivatedSceneElement : spawner.DeactivatedSceneElement;
                    contents[point.Name] = (point, elements);
                }
            }

            foreach (var (point, elements) in contents.Values)
            {
                ct.ThrowIfCancellationRequested();
                var path = FProceduralGameplayLayout.SelectBlueprint(elements, default, ref random);
                if (path.TryLoad<UBlueprintGeneratedClass>(out var blueprint))
                    result.Add(new FProceduralActorPlacement($"{host.Name}_{point.Name}", blueprint, point.Transform));
            }

            host.SpawnerActivation = activation;
            host.SpawnerElements = overrides;
        }

        return result;
    }

    private static FActorSpawnerProperties[]? GetElements(FSpawnerElement[] lists, int index) => lists.Length == 0 ? null : lists[Math.Min(index, lists.Length - 1)].ObjectsToBeSpawned;
}
