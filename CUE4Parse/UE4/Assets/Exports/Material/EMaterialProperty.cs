using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public enum EMaterialProperty : int
{
    [Description("Emissive")]
    MP_EmissiveColor = 0,
    [Description("Opacity")]
    MP_Opacity,
    [Description("Opacity Mask")]
    MP_OpacityMask,
    MP_DiffuseColor,			// used in Lightmass, not exposed to user, computed from: BaseColor, Metallic				Also used in Substrate which uses Albedo/F0 parameterization
    MP_SpecularColor,			// used in Lightmass, not exposed to user, derived from: SpecularColor, Metallic, Specular	Also used in Substrate which uses Albedo/F0 parameterization
    [Description("Diffuse")]
    MP_BaseColor,
    [Description("Metallic")]
    MP_Metallic,
    [Description("Specular")]
    MP_Specular,
    [Description("Roughness")]
    MP_Roughness,
    [Description("Anisotropy")]
    MP_Anisotropy,
    [Description("Normal")]
    MP_Normal,
    [Description("Tangent")]
    MP_Tangent,
    [Description("World Position Offset")]
    MP_WorldPositionOffset,
    MP_WorldDisplacement_DEPRECATED,
    MP_TessellationMultiplier_DEPRECATED,
    [Description("Subsurface")]
    MP_SubsurfaceColor,
    MP_CustomData0,
    MP_CustomData1,
    [Description("Ambient Occlusion")]
    MP_AmbientOcclusion,
    [Description("Refraction")]
    MP_Refraction,
    MP_CustomizedUVs0,
    MP_CustomizedUVs1,
    MP_CustomizedUVs2,
    MP_CustomizedUVs3,
    MP_CustomizedUVs4,
    MP_CustomizedUVs5,
    MP_CustomizedUVs6,
    MP_CustomizedUVs7,
    MP_PixelDepthOffset,
    MP_ShadingModel,
    [Description("Front Material")]
    MP_FrontMaterial,
    MP_SurfaceThickness,
    MP_Displacement,

    //^^^ New material properties go above here ^^^^
    MP_MaterialAttributes,
    MP_CustomOutput,
    MP_MAX,

    // Aliases
    MP_LastCustomizedUVs = MP_CustomizedUVs7,
    MP_NumCustomizedUVs = MP_LastCustomizedUVs - MP_CustomizedUVs0 + 1,
}
