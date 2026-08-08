using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component.Landscape;

public class ULandscapeHeightfieldCollisionComponent : USceneComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game == GAME_WorldofJadeDynasty) Ar.Position += 16;

        if (Ar.Ver < EUnrealEngineObjectUE4Version.LANDSCAPE_COLLISION_DATA_COOKING && (Ar.IsFilterEditorOnly || Ar.Game < GAME_UE4_0))
        {
            new FWordBulkData(Ar); // CollisionHeightData
            if (Ar.Ver >= EUnrealEngineObjectUE3Version.LANDSCAPE_PHYS_MATERIALS)
            {
                new FByteBulkData(Ar); // DominantLayerData
            }
        }
        else
        {
            var bCooked = Ar.ReadBoolean();
            if (bCooked)
            {
                if (Ar.Game >= GAME_UE4_14)
                    if (Ar.Game == GAME_PlayerUnknownsBattlegrounds)
                        _ = new FByteBulkData(Ar);
                    else
                        Ar.SkipBulkArrayData(); // CookedCollisionData
                else
                    Ar.SkipFixedArray(1);
            }
        }
    }
}
