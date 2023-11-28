using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Material;

public enum ETranslucencyLightingMode : byte
{
    [Description("Volumetric NonDirectional")]
    TLM_VolumetricNonDirectional,
    [Description("Volumetric Directional")]
    TLM_VolumetricDirectional,
    [Description("Volumetric PerVertex NonDirectional")]
    TLM_VolumetricPerVertexNonDirectional,
    [Description("Volumetric PerVertex Directional")]
    TLM_VolumetricPerVertexDirectional,
    [Description("Surface TranslucencyVolume")]
    TLM_Surface,
    [Description("Surface ForwardShading")]
    TLM_SurfacePerPixelLighting,
    TLM_MAX
}