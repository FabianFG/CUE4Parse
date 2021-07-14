using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public class UAnimationAsset : UObject
    {
        public FGuid SkeletonGuid;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Ver >= UE4Version.VER_UE4_SKELETON_GUID_SERIALIZATION)
            {
                SkeletonGuid = Ar.Read<FGuid>();
            }
        }
    }
}