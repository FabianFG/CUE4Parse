using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public class UMaterialInstance : UMaterialInterface
    {
        public UUnrealMaterial? Parent;
        public FMaterialInstanceBasePropertyOverrides BasePropertyOverrides;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Parent = GetOrDefault<UUnrealMaterial>(nameof(Parent));
            BasePropertyOverrides = GetOrDefault<FMaterialInstanceBasePropertyOverrides>(nameof(BasePropertyOverrides));
        }
    }
}