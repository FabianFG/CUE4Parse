using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;

public class UBuildingTextureData : UDataAsset
{
    public FPackageIndex Diffuse;
    public FPackageIndex Normal;
    public FPackageIndex Specular;
    public FColor? TintColor;
    public FPackageIndex OverrideMaterial;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Diffuse = GetOrDefault(nameof(Diffuse), new FPackageIndex());
        Normal = GetOrDefault(nameof(Normal), new FPackageIndex());
        Specular = GetOrDefault(nameof(Specular), new FPackageIndex());
        TintColor = GetOrDefault<FColor?>(nameof(TintColor));
        OverrideMaterial = GetOrDefault(nameof(OverrideMaterial), new FPackageIndex());
    }
}
