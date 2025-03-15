using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class ADecalActor : AActor {
    private FPackageIndex? Decal;
    public FVector DecalSize;

    public override void Deserialize(FAssetArchive Ar, long validPos) {
        base.Deserialize(Ar, validPos);
        Decal = GetOrDefault<FPackageIndex>(nameof(Decal));
        DecalSize = GetOrDefault<FVector>(nameof(DecalSize), new FVector(128.0f, 256.0f, 256.0f));
    }

    public UMaterialInterface? GetDecalMaterial() {
        return Decal?.Load<UDecalComponent>()?.GetDecalMaterial();
    }
}