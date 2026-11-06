using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal static class FProceduralBasementLayout
{
    public static IReadOnlyList<FProceduralActorPlacement> Build(IEnumerable<FProceduralActorPlacement> placements, int seed, CancellationToken ct)
    {
        var spawnersByBlueprint = new Dictionary<UBlueprintGeneratedClass, IReadOnlyList<(UActorSpawner Spawner, FTransform Transform)>>();
        var hosts = placements.Select(placement =>
        {
            ct.ThrowIfCancellationRequested();
            if (!spawnersByBlueprint.TryGetValue(placement.Blueprint, out var spawners))
            {
                spawners = FProceduralComponentReader.ReadSpawners(placement.Blueprint);
                spawnersByBlueprint.Add(placement.Blueprint, spawners);
            }

            return (Placement: placement, Spawners: spawners);
        }).ToArray();

        var candidates = hosts.SelectMany((host, hostIndex) => host.Spawners
            .Where(entry => entry.Spawner is { Type: EGameplayElementType.MeatLocker_Big, SpawnCountMax: not 0 } && entry.Spawner.ActivatedSceneElement.Any(element => element.Weight > 0 && !element.SceneElement.AssetPathName.IsNone))
            .Select(entry => (HostIndex: hostIndex, Point: new FProceduralSpawnPoint($"{host.Placement.Name}_{entry.Spawner.Name}", entry.Spawner, entry.Transform * host.Placement.Transform))))
            .ToArray();

        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.BasementSelection);

        var basement = FProceduralPopulation.Select(candidates.Select(candidate => candidate.Point), 1, 1, 0, ref random, ct).FirstOrDefault();
        var basementHost = basement == null ? -1 : candidates.First(candidate => candidate.Point == basement).HostIndex;
        var variationRandom = new FProceduralRandomStream(seed, EProceduralRandomStream.ActorSelection);
        var result = new List<FProceduralActorPlacement>();
        if (basement?.Component is UActorSpawner basementSpawner && LoadSceneElement(basementSpawner.ActivatedSceneElement) is { } basementBlueprint)
        {
            result.Add(new FProceduralActorPlacement("Basement", basementBlueprint, basement.Transform));
        }

        for (var hostIndex = 0; hostIndex < hosts.Length; hostIndex++)
        {
            var host = hosts[hostIndex];
            foreach (var (spawner, transform) in host.Spawners)
            {
                ct.ThrowIfCancellationRequested();
                if (spawner.Type != EGameplayElementType.Basement_Attachments)
                    continue;

                var elements = hostIndex == basementHost ? spawner.ActivatedSceneElement : spawner.DeactivatedSceneElement;
                if (LoadSceneElement(elements) is { } blueprint)
                {
                    result.Add(new FProceduralActorPlacement($"{host.Placement.Name}_{spawner.Name}", blueprint, transform * host.Placement.Transform));
                }
            }
        }

        return result;

        UBlueprintGeneratedClass? LoadSceneElement(FActorSpawnerProperties[] elements)
        {
            ct.ThrowIfCancellationRequested();
            var path = FProceduralGameplayLayout.SelectBlueprint(elements, default, ref variationRandom);
            return path.TryLoad<UBlueprintGeneratedClass>(out var blueprint) ? blueprint : null;
        }
    }
}
