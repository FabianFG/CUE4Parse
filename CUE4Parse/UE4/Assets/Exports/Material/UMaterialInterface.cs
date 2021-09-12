using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [SkipObjectRegistration]
    public class UMaterialInterface : UUnrealMaterial
    {
        //I think those aren't used in UE4 but who knows
        public UTexture? FlattenedTexture;
        public UTexture? MobileBaseTexture;
        public UTexture? MobileNormalTexture;
        public bool bUseMobileSpecular;
        public float MobileSpecularPower = 16.0f;
        public EMobileSpecularMask MobileSpecularMask = EMobileSpecularMask.MSM_Constant;
        public UTexture? MobileMaskTexture;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            FlattenedTexture = GetOrDefault<UTexture>(nameof(FlattenedTexture));
            MobileBaseTexture = GetOrDefault<UTexture>(nameof(MobileBaseTexture));
            MobileNormalTexture = GetOrDefault<UTexture>(nameof(MobileNormalTexture));
            bUseMobileSpecular = GetOrDefault<bool>(nameof(bUseMobileSpecular));
            MobileSpecularPower = GetOrDefault(nameof(MobileNormalTexture), 16.0f);
            MobileSpecularMask = GetOrDefault<EMobileSpecularMask>(nameof(MobileSpecularMask));
            MobileNormalTexture = GetOrDefault<UTexture>(nameof(MobileNormalTexture));
            MobileMaskTexture = GetOrDefault<UTexture>(nameof(MobileNormalTexture));
        }

        public override void GetParams(CMaterialParams parameters)
        {
            if (FlattenedTexture != null) parameters.Diffuse = FlattenedTexture;
            if (MobileBaseTexture != null) parameters.Diffuse = MobileBaseTexture;
            if (MobileNormalTexture != null) parameters.Normal = MobileNormalTexture;
            if (MobileMaskTexture != null) parameters.Opacity = MobileMaskTexture;
            parameters.UseMobileSpecular = bUseMobileSpecular;
            parameters.MobileSpecularPower = MobileSpecularPower;
            parameters.MobileSpecularMask = MobileSpecularMask;
        }
    }
}