using System.Collections.Generic;
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
        FinalRefBonePose = Ar.ReadArray(() => new FTransform(Ar));

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.REFERENCE_SKELETON_REFACTOR)
        {
            FinalNameToIndexMap = Ar.ReadMap(() => Ar.ReadFName().Text, Ar.Read<int>);
        }
        else FinalNameToIndexMap = [];

        if (Ar.Game == EGame.GAME_DaysGone) Ar.SkipFixedArray(12);

        if (Ar.Ver < EUnrealEngineObjectUE4Version.FIXUP_ROOTBONE_PARENT)
        {
            if (FinalRefBoneInfo.Length > 0 && FinalRefBoneInfo[0].ParentIndex != -1)
            {
                FinalRefBoneInfo[0] = new FMeshBoneInfo(FinalRefBoneInfo[0].Name, -1);
            }
        }

        AdjustBoneScales(FinalRefBonePose);

        if (Ar.Game == EGame.GAME_WutheringWaves)
        {
            Ar.SkipFixedArray(12);
            Ar.Position += 4;
        }
    }

    public void AdjustBoneScales(FTransform[] transforms)
    {
        if (FinalRefBoneInfo.Length != transforms.Length)
            return;

        for (var boneIndex = 0; boneIndex < transforms.Length; boneIndex++)
        {
            var scale = GetBoneScale(transforms, boneIndex);
            transforms[boneIndex].Translation.Scale(scale);
        }
    }

    public FVector GetBoneScale(FTransform[] transforms, int boneIndex)
    {
        var scale = new FVector(1);

        // Get the parent bone, ignore scale of the current one
        boneIndex = FinalRefBoneInfo[boneIndex].ParentIndex;
        while (boneIndex >= 0)
        {
            var boneScale = transforms[boneIndex].Scale3D;
            // Accumulate the scale
            scale.Scale(boneScale);
            // Get the bone's parent
            boneIndex = FinalRefBoneInfo[boneIndex].ParentIndex;
        }

        return scale;
    }
}
