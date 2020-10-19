using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    public struct FAnimCurveType
    {
        public readonly bool bMaterial;
        public readonly bool bMorphtarget;

        public FAnimCurveType(FAssetArchive Ar)
        {
            bMaterial = Ar.ReadBoolean();
            bMorphtarget = Ar.ReadBoolean();
        }
    }
}
