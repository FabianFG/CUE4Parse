using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine.Animation
{
    public class UAnimationAsset : Assets.Exports.UObject
    {
        public FGuid SkeletonGuid;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            if (Ar.Ver >= EUnrealEngineObjectUE4Version.SKELETON_GUID_SERIALIZATION)
            {
                SkeletonGuid = Ar.Read<FGuid>();
            }
        }
    }
}
