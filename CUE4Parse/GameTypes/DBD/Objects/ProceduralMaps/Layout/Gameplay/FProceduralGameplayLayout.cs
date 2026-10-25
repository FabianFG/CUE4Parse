using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Assets;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal static class FProceduralGameplayLayout
{
    public static IReadOnlyList<FProceduralActorPlacement> Build(IEnumerable<FProceduralActorPlacement> placements, FProceduralMapDefinition map, int seed, CancellationToken ct)
    {
        if (map.GenerationData is not { } generationData)
            return [];

        var spawnPoints = FProceduralSpawnPoint.Collect(placements, ct);
        var actors = new List<FProceduralActorPlacement>();
        var blueprintCache = new Dictionary<string, UBlueprintGeneratedClass?>(StringComparer.Ordinal);
        var variationRandom = new FProceduralRandomStream(seed, EProceduralRandomStream.ActorSelection);

        // Totems
        foreach (var population in generationData.ElementsPopulation.Where(population => population.Type is EGameplayElementType.Totems))
        {
            var random = new FProceduralRandomStream(seed, population.Type);
            var minimum = Math.Max(0, population.MinPopulation);
            var maximum = Math.Max(minimum, population.MaxPopulation);
            var count = minimum + (int) (random.NextFloat() * ((long) maximum - minimum + 1));
            var candidates = new FProceduralPopulation(GetSpawners(population.Type), map.PopulationSettings);
            foreach (var spawnPoint in candidates.SelectCount(count, map.PopulationSettings.MinProximityBetweenTotems, ref random, ct))
            {
                AddActor(spawnPoint, population.GameplayElementBlueprint);
            }
        }

        // Hooks/Pallets
        foreach (var population in map.ManagedElementsPopulation.Where(population => population.Type is EGameplayElementType.MeatLocker_Small or EGameplayElementType.Bookshelves))
        {
            var random = new FProceduralRandomStream(seed, population.Type);
            var minimum = Math.Max(0, population.MinPopulation);
            var maximum = Math.Max(minimum, population.MaxPopulation);
            var minimumDistance = Math.Max(0, population.MinDistance) * generationData.ProceduralTileWidth;
            var candidates = new FProceduralPopulation(GetSpawners(population.Type).Where(point => point.IsActive), map.PopulationSettings);
            if (population.Type is EGameplayElementType.MeatLocker_Small && placements.FirstOrDefault(actor => actor.Name == "Basement") is { } basement)
            {
                // The native builder measures basement influence from this rotated offset, not its spawner origin
                var position = basement.Transform.Translation + basement.Transform.Rotation.RotateVector(new FVector(330, 210, 20));
                candidates.AdjustWeights(position, map.PopulationSettings.SmallMeatLockerMinProximityFromKillerLair, map.PopulationSettings.MapWidth, 0);
            }
            foreach (var spawnPoint in candidates.Select(minimum, maximum, minimumDistance, ref random, ct))
            {
                AddActor(spawnPoint, population.GameplayElementBlueprint);
            }
        }

        // Chests
        var chestCount = GetChestCount(map, seed);
        var chestRandom = new FProceduralRandomStream(seed, EGameplayElementType.Searchable);
        chestRandom.NextFloat();
        var chests = new FProceduralPopulation(GetSpawners(EGameplayElementType.Searchable), map.PopulationSettings);
        foreach (var spawnPoint in chests.SelectCount(chestCount, map.PopulationSettings.MinProximityBetweenChests, ref chestRandom, ct))
        {
            AddActor(spawnPoint);
        }

        // Breakable walls
        foreach (var spawnPoint in GetSpawners(EGameplayElementType.BreakableWalls).Where(point => point.IsActive && point.Component is UActorSpawner { Weight: > 0 }))
        {
            AddActor(spawnPoint);
        }

        // Generators
        var generators = spawnPoints.Where(point => point.Component is UTileSpawnPoint { TileSpawnPointType: ETileSpawnPointType.InteractableObject }).ToArray();
        var generatorRandom = new FProceduralRandomStream(seed, EProceduralRandomStream.Default); // Not real randomization because match rules are needed here
        foreach (var spawnPoint in FProceduralGeneratorLayout.Select(generators, map.GeneratorCount, map.PopulationSettings, ref generatorRandom, ct))
        {
            AddActor(spawnPoint);
        }

        // Hatch
        var hatchRandom = new FProceduralRandomStream(seed, EProceduralRandomStream.HatchSelection);
        var hatches = new FProceduralPopulation(GetSpawners(EGameplayElementType.Hatch), map.PopulationSettings);
        foreach (var spawnPoint in hatches.SelectCount(map.HatchCount, map.PopulationSettings.MinProximityBetweenHatches, ref hatchRandom, ct))
        {
            AddActor(spawnPoint);
        }

        // Edge objects (ex. door frames)
        actors.AddRange(FProceduralEdgeLayout.Build(placements, spawnPoints, seed, ct));

        return actors;

        IEnumerable<FProceduralSpawnPoint> GetSpawners(EGameplayElementType type) => spawnPoints.Where(point => point.Component is UActorSpawner spawner && spawner.Type == type && spawner.SpawnCountMax != 0);

        void AddActor(FProceduralSpawnPoint spawnPoint, FSoftObjectPath fallback = default)
        {
            ct.ThrowIfCancellationRequested();
            var path = SelectBlueprint(spawnPoint, fallback, ref variationRandom);
            var classPath = path.AssetPathName.Text;
            if (!blueprintCache.TryGetValue(classPath, out var blueprint))
            {
                path.TryLoad<UBlueprintGeneratedClass>(out blueprint);
                blueprintCache.Add(classPath, blueprint);
            }

            if (blueprint != null)
                actors.Add(new FProceduralActorPlacement(spawnPoint.Name, blueprint, spawnPoint.Transform));
        }
    }

    private static int GetChestCount(FProceduralMapDefinition map, int seed)
    {
        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.ChestCount);
        var count = map.ChestPopulation?.Eval(random.NextFloat()) ?? 0;
        return Math.Max(0, (int) MathF.Round(count, MidpointRounding.AwayFromZero) + map.ChestCountModifier);
    }

    internal static FSoftObjectPath SelectBlueprint(FProceduralSpawnPoint spawnPoint, FSoftObjectPath fallback, ref FProceduralRandomStream random)
    {
        var path = fallback;
        if (spawnPoint.Component is UActorSpawner spawner)
        {
            path = SelectBlueprint(spawnPoint.Elements ?? spawner.ActivatedSceneElement, spawnPoint.Elements == null ? fallback : default, ref random);
        }
        else if (spawnPoint.Component is UTileSpawnPoint tileSpawnPoint)
        {
            path = tileSpawnPoint.Visualization;
        }

        return path;
    }

    internal static FSoftObjectPath SelectBlueprint(FActorSpawnerProperties[] elements, FSoftObjectPath fallback, ref FProceduralRandomStream random)
    {
        var candidates = elements.Where(element => float.IsFinite(element.Weight) && element.Weight > 0).ToArray();
        var remainingWeight = random.NextFloat() * candidates.Sum(element => (double) element.Weight);
        foreach (var element in candidates)
        {
            fallback = element.SceneElement;
            if (remainingWeight < element.Weight)
                break;

            remainingWeight -= element.Weight;
        }

        return fallback;
    }
}
