using CUE4Parse.GameTypes.DBD.Assets.Exports;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal sealed class FProceduralPopulationSettings(AProceduralLevelBuilder defaults, float tileSize = 1600, int mapWidth = 0)
{
    public FProceduralPopulationSettings(float tileSize = 1600, int mapWidth = 0) : this(new AProceduralLevelBuilder(), tileSize, mapWidth) { }

    public readonly float TileSize = tileSize;
    public readonly int MapWidth = mapWidth;
    public readonly float MinProximityBetweenGenerators = defaults.MinProximityBetweenGenerators;
    public readonly float MinProximityBetweenChests = defaults.MinProximityBetweenChests;
    public readonly float MinProximityBetweenTotems = defaults.MinProximityBetweenTotems;
    public readonly float MinProximityBetweenHatches = defaults.MinProximityBetweenHatches;
    public readonly float SmallMeatLockerMinProximityFromKillerLair = defaults.SmallMeatLockerMinProximityFromKillerLair;
    public readonly float FactorToAdjustToWhenOutOfProximity = defaults.FactorToAdjustToWhenOutOfProximity;
    public readonly float HeightAmplifier = defaults.HeightAmplifier;
    public readonly float HeightAmplifierActivationHeight = defaults.HeightAmplifierActivationHeight;
}
