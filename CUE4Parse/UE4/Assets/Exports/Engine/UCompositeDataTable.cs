using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Engine;

public class UCompositeDataTable : UDataTable
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Game is EGame.GAME_HonorofKingsWorld) CustomGameData = Ar.ReadArray<uint>();
        base.Deserialize(Ar, validPos);
    }
}
