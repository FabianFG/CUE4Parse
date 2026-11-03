using CUE4Parse.UE4.Assets.Exports;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

// AProceduralLevelBuilder's native defaults, overridden by the LevelGenerator Blueprint
internal sealed class FProceduralPopulationSettings(UObject? defaults = null, float tileSize = 1600, int mapWidth = 0)
{
    public readonly float TileSize = tileSize;
    public readonly int MapWidth = mapWidth;
    public readonly float MinProximityBetweenGenerators = defaults?.GetOrDefault("MinProximityBetweenGenerators", 0.5f) ?? 0.5f;
    public readonly float MinProximityBetweenChests = defaults?.GetOrDefault("MinProximityBetweenChests", 1f) ?? 1f;
    public readonly float MinProximityBetweenTotems = defaults?.GetOrDefault("MinProximityBetweenTotems", 1f) ?? 1f;
    public readonly float MinProximityBetweenHatches = defaults?.GetOrDefault("MinProximityBetweenHatches", 1f) ?? 1f;
    public readonly float SmallMeatLockerMinProximityFromKillerLair = defaults?.GetOrDefault("SmallMeatLockerMinProximityFromKillerLair", 1.5f) ?? 1.5f;
    public readonly float FactorToAdjustToWhenOutOfProximity = defaults?.GetOrDefault("FactorToAdjustToWhenOutOfProximity", 0f) ?? 0f;
    public readonly float HeightAmplifier = defaults?.GetOrDefault("HeightAmplifier", 3f) ?? 3f;
    public readonly float HeightAmplifierActivationHeight = defaults?.GetOrDefault("HeightAmplifierActivationHeight", 350f) ?? 350f;
}
