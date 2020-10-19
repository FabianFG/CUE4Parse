using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class FMeshBoneInfo
    {
        public readonly FName Name;
        public readonly int ParentIndex;

        public FMeshBoneInfo(FAssetArchive Ar)
        {
            Name = Ar.ReadFName();
            ParentIndex = Ar.Read<int>();

            if (Ar.Ver < Versions.UE4Version.VER_UE4_REFERENCE_SKELETON_REFACTOR)
            {
                Ar.Read<FColor>();
            }
        }

        public FMeshBoneInfo(FName name, int parentIndex)
        {
            Name = name;
            ParentIndex = parentIndex;
        }
    }
}
