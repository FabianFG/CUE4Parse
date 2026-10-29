using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports;

public class AProceduralLevelBuilder : AActor
{
    public string PathToTilesDirectory = string.Empty;
    public float QuadTileWeightRateOfDecay = 0.01f;
    public float MinProximityBetweenGenerators = 0.5f;
    public float MinProximityBetweenChests = 1f;
    public float MinProximityBetweenTotems = 1f;
    public float MinProximityBetweenHatches = 1f;
    public float SmallMeatLockerMinProximityFromKillerLair = 1.5f;
    public float FactorToAdjustToWhenOutOfProximity;
    public float HeightAmplifier = 3f;
    public float HeightAmplifierActivationHeight = 350f;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        PathToTilesDirectory = GetOrDefault(nameof(PathToTilesDirectory), PathToTilesDirectory);
        QuadTileWeightRateOfDecay = GetOrDefault(nameof(QuadTileWeightRateOfDecay), QuadTileWeightRateOfDecay);
        MinProximityBetweenGenerators = GetOrDefault(nameof(MinProximityBetweenGenerators), MinProximityBetweenGenerators);
        MinProximityBetweenChests = GetOrDefault(nameof(MinProximityBetweenChests), MinProximityBetweenChests);
        MinProximityBetweenTotems = GetOrDefault(nameof(MinProximityBetweenTotems), MinProximityBetweenTotems);
        MinProximityBetweenHatches = GetOrDefault(nameof(MinProximityBetweenHatches), MinProximityBetweenHatches);
        SmallMeatLockerMinProximityFromKillerLair = GetOrDefault(nameof(SmallMeatLockerMinProximityFromKillerLair), SmallMeatLockerMinProximityFromKillerLair);
        FactorToAdjustToWhenOutOfProximity = GetOrDefault(nameof(FactorToAdjustToWhenOutOfProximity), FactorToAdjustToWhenOutOfProximity);
        HeightAmplifier = GetOrDefault(nameof(HeightAmplifier), HeightAmplifier);
        HeightAmplifierActivationHeight = GetOrDefault(nameof(HeightAmplifierActivationHeight), HeightAmplifierActivationHeight);
    }
}

public class ALevelGenerator_C : AProceduralLevelBuilder;
