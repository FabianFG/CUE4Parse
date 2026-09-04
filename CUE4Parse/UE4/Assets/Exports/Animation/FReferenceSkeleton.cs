using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

[JsonConverter(typeof(FReferenceSkeletonConverter))]
public class FReferenceSkeleton
{
    public readonly FMeshBoneInfo[] FinalRefBoneInfo;
    public readonly FTransform[] FinalRefBonePose;
    public readonly Dictionary<string, int> FinalNameToIndexMap;

    public FReferenceSkeleton(FAssetArchive Ar)
    {
        FinalRefBoneInfo = Ar.ReadArray(() => new FMeshBoneInfo(Ar));
        if (Ar.Game < GAME_UE4_0)
        {
            FinalRefBonePose = new FTransform[FinalRefBoneInfo.Length];
            for (int i = 0; i < FinalRefBoneInfo.Length; i++)
            {
                FinalRefBonePose[i] = new FTransform(FinalRefBoneInfo[i].BonePos.Orientation, FinalRefBoneInfo[i].BonePos.Position, FVector.OneVector);
            }
        }
        else
        {
            FinalRefBonePose = Ar.ReadArray(() => new FTransform(Ar));
        }

        FinalNameToIndexMap = Ar.Ver >= EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR ? Ar.ReadMap(() => Ar.ReadFName().Text, Ar.Read<int>) : [];

        if (Ar.Game == GAME_DaysGone) Ar.SkipFixedArray(12);

        if (Ar.Ver < EUnrealEngineObjectUE4Version.FIXUP_ROOTBONE_PARENT)
        {
            if (FinalRefBoneInfo.Length > 0 && FinalRefBoneInfo[0].ParentIndex != -1)
            {
                FinalRefBoneInfo[0] = new FMeshBoneInfo(FinalRefBoneInfo[0].Name, -1);
            }
        }

        if (Ar.Game == GAME_WutheringWaves)
        {
            Ar.SkipFixedArray(12);
            Ar.Position += 4;
        }
    }
}
