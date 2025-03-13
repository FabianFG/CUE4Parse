using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component;


public class UDecalComponent : USceneComponent {
    public FPackageIndex? DecalMaterial;
    public FVector? DecalSize;

    public override void Deserialize(FAssetArchive Ar, long validPos) {
        base.Deserialize(Ar, validPos);
        
        if (Ar.Ver < EUnrealEngineObjectUE4Version.DECAL_SIZE)
            DecalSize = FVector.OneVector;
        DecalMaterial = GetOrDefault<FPackageIndex>(nameof(DecalMaterial), new FPackageIndex());
        DecalSize = GetOrDefault<FVector>(nameof(DecalSize));
    }

    public UMaterialInterface? GetDecalMaterial() {
        if (DecalMaterial == null) return null;
        return DecalMaterial?.Load<UMaterialInterface>();
    }
}