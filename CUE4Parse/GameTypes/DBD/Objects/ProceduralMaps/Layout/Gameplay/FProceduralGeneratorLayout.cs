using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal static class FProceduralGeneratorLayout
{
    public static List<FProceduralSpawnPoint> Select(FProceduralSpawnPoint[] points, int count, FProceduralPopulationSettings settings, ref FProceduralRandomStream random, CancellationToken ct)
    {
        var selected = new List<FProceduralSpawnPoint>();
        var minimumDistance = settings.TileSize * settings.MinProximityBetweenGenerators;
        for (var attempt = 0; attempt < points.Length && selected.Count < count; attempt++)
        {
            var population = new FProceduralPopulation(points, settings);
            var candidates = population.Select(0, count, minimumDistance, ref random, ct);
            if (candidates.Count > selected.Count)
                selected = candidates;
        }

        return selected;
    }

    public static void Apply(IReadOnlyList<FProceduralTilePlacement> tiles, IReadOnlyList<FProceduralActorPlacement> gameplay, int seed, CancellationToken ct)
    {
        var spawned = gameplay.Select(actor => actor.Name).ToHashSet(StringComparer.Ordinal);
        var rulesByBlueprint = new Dictionary<UBlueprintGeneratedClass, List<FProceduralGeneratorReader.FRule>>();
        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.ActorSelection);
        foreach (var tile in tiles)
        {
            ct.ThrowIfCancellationRequested();
            if (!rulesByBlueprint.TryGetValue(tile.Tile.Blueprint, out var rules))
                rulesByBlueprint.Add(tile.Tile.Blueprint, rules = FProceduralGeneratorReader.Read(tile.Tile.Blueprint));
            if (rules.Count == 0)
                continue;

            var tags = FProceduralComponentReader.ReadComponents<UTileSpawnPoint>(tile.Tile.Blueprint)
                .Where(point => spawned.Contains($"Tile_{tile.X}_{tile.Y}_{point.Component.Name}"))
                .SelectMany(point => point.Component.GetOrDefault<FName[]>("ComponentTags", []))
                .ToHashSet();

            var variations = tile.ComponentVariations?.ToDictionary() ?? new Dictionary<string, UBlueprintGeneratedClass>(StringComparer.Ordinal);
            var activation = tile.SpawnerActivation?.ToDictionary() ?? new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                if (tags.Contains(rule.Tag) != rule.WhenGeneratorSpawned)
                    continue;

                var path = rule.WhenGeneratorSpawned ? FProceduralGameplayLayout.SelectBlueprint(rule.Elements, default, ref random) : default;
                if (path.TryLoad<UBlueprintGeneratedClass>(out var blueprint))
                {
                    variations[rule.Component] = blueprint;
                    activation.Remove(rule.Component);
                }
                else
                {
                    variations.Remove(rule.Component);
                    activation[rule.Component] = false;
                }
            }

            tile.ComponentVariations = variations.Count > 0 ? variations : null;
            tile.SpawnerActivation = activation.Count > 0 ? activation : null;
        }
    }
}
