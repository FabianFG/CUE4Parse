using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Assets;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Quadrants;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout;

public sealed class FProceduralLevelLayout
{
    private readonly FProceduralMapDefinition _definition;
    public readonly int Width;
    public readonly int Height;
    public readonly int Seed;
    public readonly FVector2D TileSize;
    public readonly IReadOnlyList<FProceduralTilePlacement> Placements;
    public readonly IReadOnlyList<FProceduralActorPlacement> Quadrants;
    public readonly IReadOnlyList<FProceduralActorPlacement> BasementActors;
    public readonly IReadOnlyList<FProceduralActorPlacement> GameplayActors;
    public readonly UBlueprintGeneratedClass? LightingBlueprint;
    public FTransform EnvironmentTransform => FProceduralTileLayout.EnvironmentTransform(TileSize, Width, Height);

    public FProceduralLevelLayout(UPaperTileMap map, int seed = 1, CancellationToken ct = default) : this(new FProceduralMapDefinition(map, ct), seed, ct) { }

    private FProceduralLevelLayout(FProceduralMapDefinition map, int seed, CancellationToken ct)
    {
        _definition = map;
        ct.ThrowIfCancellationRequested();
        Width = map.Width;
        Height = map.Height;
        Seed = seed;
        TileSize = map.TileSize;
        Placements = FProceduralTileLayout.Build(map.Cells, Width, Height, map.Tiles, seed, map.EscapeDoorCount, ct);
        FProceduralBorderLayout.Build(Placements, map.Cells, Width, Height, ct);
        FProceduralVariationLayout.Build(Placements, map.Cells, Width, ct);
        Quadrants = map.GenerationData == null ? [] : FProceduralQuadrantPlacementBuilder.Load(map.Cells, Width, Height, TileSize, Placements, map.GenerationData, seed, map.QuadTileWeightRateOfDecay, ct);

        var hosts = Placements.Select(placement => new FProceduralActorPlacement($"Tile_{placement.X}_{placement.Y}", placement.Tile.Blueprint, placement.Transform(TileSize, Width, Height))
            { SpawnerActivation = placement.SpawnerActivation, ComponentVariations = placement.ComponentVariations }).Concat(Quadrants).ToArray();
        BasementActors = FProceduralBasementLayout.Build(hosts, seed, ct);

        var randomizedActors = FProceduralRandomizerLayout.Build(hosts, seed, ct);
        for (var index = 0; index < Placements.Count; index++)
            Placements[index].SpawnerActivation = hosts[index].SpawnerActivation;

        GameplayActors = [.. FProceduralGameplayLayout.Build(hosts.Concat(BasementActors).Concat(randomizedActors), map, seed, ct), .. randomizedActors];
        FProceduralGeneratorLayout.Apply(Placements, GameplayActors, seed, ct);

        LightingBlueprint = map.LightingBlueprint;
#if DEBUG
        Log.Debug("Generated DBD map {Map}: {Tiles} tiles on a {Width}x{Height} grid", map.Name, Placements.Count, Width, Height);
#endif
    }

    public FProceduralLevelLayout Regenerate(int seed, CancellationToken ct = default) => new(_definition, seed, ct);
}
