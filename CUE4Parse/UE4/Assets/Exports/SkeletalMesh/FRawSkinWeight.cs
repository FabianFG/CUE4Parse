using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public static class FRawSkinWeight
{
    public static FSkinWeightInfo Serialize(FAssetArchive Ar)
    {
        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences)
        {
            var bUse16BitBoneIndex = FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.IncreaseBoneIndexLimitPerChunk;
            return new FSkinWeightInfo(Ar, true, bUse16BitBoneIndex, false);
        }
        else if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.IncreasedSkinWeightPrecision)
        {
            return new FSkinWeightInfo(Ar, true, true, false, FSkinWeightInfo.MAX_TOTAL_INFLUENCES);
        }
        else
        {
            return new FSkinWeightInfo(Ar, true, true, true, FSkinWeightInfo.MAX_TOTAL_INFLUENCES);
        }
    }
}
