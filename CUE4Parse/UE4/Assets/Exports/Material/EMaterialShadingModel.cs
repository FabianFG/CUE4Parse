using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public enum EMaterialShadingModel
    {
        [Description("Unlit")]
        MSM_Unlit,
        [Description("Default Lit")]
        MSM_DefaultLit,
        [Description("Subsurface")]
        MSM_Subsurface,
        [Description("Preintegrated Skin")]
        MSM_PreintegratedSkin,
        [Description("Clear Coat")]
        MSM_ClearCoat,
        [Description("Subsurface Profile")]
        MSM_SubsurfaceProfile,
        [Description("Two Sided Foliage")]
        MSM_TwoSidedFoliage,
        [Description("Hair")]
        MSM_Hair,
        [Description("Cloth")]
        MSM_Cloth,
        [Description("Eye")]
        MSM_Eye,
        [Description("SingleLayerWater")]
        MSM_SingleLayerWater,
        [Description("Thin Translucent")]
        MSM_ThinTranslucent,
        [Description("Strata")]
        MSM_Strata,
        /** Number of unique shading models. */
        [Description("NUM")]
        MSM_NUM,
        /** Shading model will be determined by the Material Expression Graph,
		by utilizing the 'Shading Model' MaterialAttribute output pin. */
        [Description("From Material Expression")]
        MSM_FromMaterialExpression,
        [Description("MAX")]
        MSM_MAX
    }
}
