using System.ComponentModel;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    public enum EBlendMode : byte
    {
        [Description("Opaque")]
        BLEND_Opaque,
        [Description("Masked")]
        BLEND_Masked,
        [Description("Translucent")]
        BLEND_Translucent,
        [Description("Additive")]
        BLEND_Additive,
        [Description("Modulate")]
        BLEND_Modulate,
        [Description("AlphaComposite (Premultiplied Alpha)")]
        BLEND_AlphaComposite,
        [Description("AlphaHoldout")]
        BLEND_AlphaHoldout,
        BLEND_TranslucentColoredTransmittance,
        [Description("MAX")]
        BLEND_MAX,

        BLEND_TranslucentGreyTransmittance = BLEND_Translucent,
        BLEND_ColoredTransmittanceOnly = BLEND_Modulate,
    }
}
