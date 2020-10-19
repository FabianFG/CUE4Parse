using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public struct FReferencePose
    {
        public readonly FName PoseName;
        public readonly FTransform[] ReferencePose;

        public FReferencePose(FAssetArchive Ar)
        {
            PoseName = Ar.ReadFName();
            ReferencePose = Ar.ReadArray<FTransform>();
        }
    }
}
