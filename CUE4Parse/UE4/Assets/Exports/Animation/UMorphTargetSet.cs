using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UMorphTargetSet : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.SERIALIZE_MORPHTARGETRAWVERTSINDICES && Ar.Game < EGame.GAME_UE4_0)
        {
            Ar.ReadArray<int>(); // RawWedgePointIndices
        }
    }
}
