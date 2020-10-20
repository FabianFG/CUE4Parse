using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Materials
{
    public class UMaterialInstance : UMaterialInterface
    {
        public UUnrealMaterial? Parent;
        public FMaterialInstanceBasePropertyOverrides BasePropertyOverrides;

        public UMaterialInstance() { }

        public UMaterialInstance(FObjectExport export) : base(export) { }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Parent = GetOrDefault<UUnrealMaterial>(nameof(Parent));
            BasePropertyOverrides = GetOrDefault<FMaterialInstanceBasePropertyOverrides>(nameof(BasePropertyOverrides));
        }
    }
}