using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Materials
{
    public class CMaterialParams
    {
        // textures
        public UUnrealMaterial? Diffuse = null;
        public UUnrealMaterial? Normal = null;
        public UUnrealMaterial? Specular = null;
        public UUnrealMaterial? SpecPower = null;
        public UUnrealMaterial? Opacity = null;
        public UUnrealMaterial? Emissive = null;
        public UUnrealMaterial? Cube = null;
        public UUnrealMaterial? Mask = null;         // multiple mask textures baked into a single one
        // channels (used with Mask texture)
        public ETextureChannel EmissiveChannel = ETextureChannel.TC_NONE;
        public ETextureChannel SpecularMaskChannel = ETextureChannel.TC_NONE;
        public ETextureChannel SpecularPowerChannel = ETextureChannel.TC_NONE;
        public ETextureChannel CubemapMaskChannel = ETextureChannel.TC_NONE;
        // colors
        public FLinearColor EmissiveColor = new FLinearColor(0.5f, 0.5f, 1.0f, 1f);       // light-blue color
        // mobile
        public bool UseMobileSpecular = false;
        public float MobileSpecularPower = 0.0f;
        public EMobileSpecularMask MobileSpecularMask = EMobileSpecularMask.MSM_Constant;
        // tweaks
        public bool SpecularFromAlpha = false;
        public bool OpacityFromAlpha = false;
        
        public bool IsNull => Diffuse == null && Normal == null && Specular == null && SpecPower == null &&
                              Opacity == null && Emissive == null && Cube == null && Mask == null;
        
        public void AppendAllTextures(IList<UUnrealMaterial> outTextures)
        {
            if (Diffuse != null) outTextures.Add(Diffuse);
            if (Normal != null) outTextures.Add(Normal);
            if (Specular != null) outTextures.Add(Specular);
            if (SpecPower != null) outTextures.Add(SpecPower);
            if (Opacity != null) outTextures.Add(Opacity);
            if (Emissive != null) outTextures.Add(Emissive);
            if (Cube != null) outTextures.Add(Cube);
            if (Mask != null) outTextures.Add(Mask);
        }
    }
}