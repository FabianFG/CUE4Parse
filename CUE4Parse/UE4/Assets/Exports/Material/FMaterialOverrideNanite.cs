using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public readonly struct FMaterialOverrideNanite : IUStruct
    {
        public readonly FSoftObjectPath OverrideMaterialRef;
        public readonly bool bEnableOverride;
        public readonly FPackageIndex OverrideMaterial;

        public FMaterialOverrideNanite(FAssetArchive Ar)
        {
            OverrideMaterialRef = new FSoftObjectPath(Ar);
            bEnableOverride = Ar.ReadBoolean();
            OverrideMaterial = new FPackageIndex(Ar);
        }
    }
}
