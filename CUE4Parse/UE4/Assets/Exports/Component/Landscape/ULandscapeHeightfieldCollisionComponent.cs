using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeHeightfieldCollisionComponent : USceneComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 16;
        var bCooked = Ar.ReadBoolean();
        if (bCooked)
        {
            if (Ar.Game >= Versions.EGame.GAME_UE4_14)
                Ar.SkipBulkArrayData(); // CookedCollisionData
            else
                Ar.SkipFixedArray(1);
        }
    }
}
