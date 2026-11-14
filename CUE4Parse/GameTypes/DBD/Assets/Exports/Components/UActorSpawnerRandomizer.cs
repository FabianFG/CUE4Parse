using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UActorSpawnerRandomizer : UObjectRandomizer
{
    public FSpawnerElement[] ActivatedSpawnerContentLists = [];
    public FSpawnerElement[] DeactivatedSpawnerContentLists = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        ActivatedSpawnerContentLists = GetOrDefault(nameof(ActivatedSpawnerContentLists), ActivatedSpawnerContentLists);
        DeactivatedSpawnerContentLists = GetOrDefault(nameof(DeactivatedSpawnerContentLists), DeactivatedSpawnerContentLists);
    }
}
