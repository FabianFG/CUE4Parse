using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UObjectRandomizer : USceneComponent
{
    public FPackageIndex[] Spawners = [];
    public int MinPopulation;
    public int MaxPopulation;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        Spawners = GetOrDefault(nameof(Spawners), Spawners);
        MinPopulation = GetOrDefault(nameof(MinPopulation), MinPopulation);
        MaxPopulation = GetOrDefault(nameof(MaxPopulation), MaxPopulation);
    }
}
