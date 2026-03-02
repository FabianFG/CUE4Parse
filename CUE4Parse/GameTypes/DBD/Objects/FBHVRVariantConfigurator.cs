using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects;

public readonly struct FBHVRVariantConfigurator(FAssetArchive Ar) : IUStruct
{
    public readonly FPackageIndex VariantGenerator = new(Ar);
    public readonly string VariantUniqueName = Ar.ReadFString();
    public readonly FGameplayTag VariantTag = new(Ar);
}
