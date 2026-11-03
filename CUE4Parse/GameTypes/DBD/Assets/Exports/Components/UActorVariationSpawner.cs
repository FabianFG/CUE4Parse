using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UActorVariationSpawner : USceneComponent
{
    public FSoftObjectPath Visualization;
    public FActorVariationElements[] Variations = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        Visualization = GetOrDefault(nameof(Visualization), Visualization);
        Variations = GetOrDefault(nameof(Variations), Variations);
    }
}
