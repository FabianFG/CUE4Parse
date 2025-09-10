using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Foliage;

public class UFoliageType : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if(Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 24;
        base.Deserialize(Ar, validPos);
    }
}
