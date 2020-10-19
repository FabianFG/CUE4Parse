using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using System.Collections.Generic;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FReferenceSkeleton
    {
        public readonly FMeshBoneInfo[] FinalRefBoneInfo;
        public readonly FTransform[] FinalRefBonePose;
        public readonly Dictionary<FName, int>? FinalNameToIndexMap;

        public FReferenceSkeleton(FAssetArchive Ar)
        {
            FinalRefBoneInfo = Ar.ReadArray(Ar.Read<int>(), () => new FMeshBoneInfo(Ar));
            FinalRefBonePose = Ar.ReadArray<FTransform>();

            if (Ar.Ver >= UE4Version.VER_UE4_REFERENCE_SKELETON_REFACTOR)
            {
                var num = Ar.Read<int>();
                FinalNameToIndexMap = new Dictionary<FName, int>(num);
                for (var i = 0; i < num; ++i)
                {
                    FinalNameToIndexMap[Ar.ReadFName()] = Ar.Read<int>();
                }
            }

            if (Ar.Ver < UE4Version.VER_UE4_FIXUP_ROOTBONE_PARENT)
            {
                if (FinalRefBoneInfo.Length > 0 && FinalRefBoneInfo[0].ParentIndex != -1)
                {
                    FinalRefBoneInfo[0] = new FMeshBoneInfo(FinalRefBoneInfo[0].Name, - 1);
                }
            }
        }
    }
}
