using CUE4Parse.GameTypes.DBD.Assets.Exports;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Assets;

internal sealed class FProceduralMapDefinition
{
    public readonly string Name;
    public readonly int Width;
    public readonly int Height;
    public readonly FVector2D TileSize;
    public readonly FProceduralTileCell[] Cells;
    public readonly IReadOnlyList<FProceduralTile> Tiles;
    public readonly UProceduralGenerationData? GenerationData;
    public readonly FProceduralPopulationSettings PopulationSettings;
    public readonly float QuadTileWeightRateOfDecay;
    public readonly IReadOnlyList<FManagedGameplayElementsPopulation> ManagedElementsPopulation;
    public readonly UBlueprintGeneratedClass? LightingBlueprint;
    public readonly int EscapeDoorCount;
    public readonly int GeneratorCount;
    public readonly int HatchCount;
    public readonly FRichCurve? ChestPopulation;
    public readonly int ChestCountModifier;

    public FProceduralMapDefinition(UPaperTileMap map, CancellationToken ct)
    {
        if (map.Game is not GAME_DeadByDaylight)
            throw new NotSupportedException("Procedural level generation is only supported for Dead by Daylight.");

        ct.ThrowIfCancellationRequested();
        var tileLayers = map.TileLayers.Select(layer => layer.Load<UPaperTileLayer>()).OfType<UPaperTileLayer>().ToArray();
        var layers = tileLayers.ToDictionary(layer => layer.LayerName.Text, StringComparer.OrdinalIgnoreCase);
        if (!layers.TryGetValue("Base", out var baseLayer))
            throw new InvalidDataException("Tile map has no Base layer.");

        Name = map.Name;
        Width = baseLayer.LayerWidth;
        Height = baseLayer.LayerHeight;
        if (Width <= 0 || Height <= 0 || (long) Width * Height > 1_000_000)
            throw new InvalidDataException($"Invalid tile map dimensions: {Width}x{Height}.");

        var paths = layers.GetValueOrDefault("Paths");
        var densities = layers.GetValueOrDefault("Types");
        var numbers = layers.GetValueOrDefault("Numbers");
        var tags = layers.GetValueOrDefault("Tags");
        // DBD reads room orientation from the optional sixth layer, separately from variation tags
        var quadrantRotations = tileLayers.ElementAtOrDefault(5);
        Cells = new FProceduralTileCell[Width * Height];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                ct.ThrowIfCancellationRequested();
                Cells[y * Width + x] = new FProceduralTileCell(
                    baseLayer.GetCell(x, y),
                    paths?.GetCell(x, y) ?? default,
                    numbers?.GetCell(x, y) ?? default,
                    tags?.GetCell(x, y) ?? default,
                    densities?.GetCell(x, y) ?? default,
                    quadrantRotations?.GetCell(x, y) ?? default
                );
            }
        }

        var provider = map.Owner!.Provider!;
        var levelGenerator = provider.SafeLoadPackageObject<AProceduralLevelBuilder>("/Game/ProceduralLevelGeneration/LevelGenerator.Default__LevelGenerator_C");
        if (levelGenerator == null || string.IsNullOrEmpty(levelGenerator.PathToTilesDirectory))
            throw new InvalidDataException("LevelGenerator has no PathToTilesDirectory.");

        var assets = new FProceduralMapAssets(provider, levelGenerator.PathToTilesDirectory);
        var theme = assets.ResolveTheme(map.Owner.Name, ct);
        Tiles = assets.LoadTiles(theme.TilePath, ct);
        GenerationData = provider.SafeLoadPackageObject<UProceduralGenerationData>("/Game/ProceduralLevelGeneration/ProceduralGenerationData.ProceduralGenerationData");

        PopulationSettings = new FProceduralPopulationSettings(levelGenerator, GenerationData?.ProceduralTileWidth ?? 1600, Width);
        QuadTileWeightRateOfDecay = levelGenerator.QuadTileWeightRateOfDecay;

        var population = GenerationData?.ManagedElementsPopulation ?? [];
        ManagedElementsPopulation = assets.LoadMap(map.Name, ct) is { } mapData
            ? [.. population.Select(element => new FManagedGameplayElementsPopulation(element, mapData))]
            : population;

        TileSize = new FVector2D(GenerationData?.ProceduralTileWidth ?? 1600, GenerationData?.ProceduralTileHeight ?? 1600);
        LightingBlueprint = assets.LoadLighting(theme.Name, ct);
        EscapeDoorCount = assets.LoadEscapeDoorCount();
        GeneratorCount = assets.LoadGeneratorCount();
        HatchCount = assets.LoadHatchCount();
        ChestPopulation = GenerationData?.SurvivorItemPopulationProbability?.Load<UCurveFloat>()?.GetOrDefault<FRichCurve>("FloatCurve");
        ChestCountModifier = assets.LoadChestCountModifier();
    }
}
