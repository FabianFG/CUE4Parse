using System;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Component.Lights;

public static class LightUtils
{
    public static float EV100ToLuminance(float ev100, float luminanceMax = 1.0f)
    {
        return (float)(luminanceMax * Math.Pow(2.0f, ev100));
    }

    public static float LuminanceToEV100(float luminance, float luminanceMax = 1.0f)
    {
        return MathF.Log2(luminance / luminanceMax);
    }

    public static float ConvertToIntensityToNits(float intensity, float areaInSqMeters, float angle, ELightUnits units)
    {
        return units switch {
            ELightUnits.Candelas => intensity / areaInSqMeters,
            ELightUnits.Lumens => intensity / (angle * areaInSqMeters),
            ELightUnits.EV => EV100ToLuminance(intensity),
            ELightUnits.Nits => intensity,
            ELightUnits.Unitless => (intensity / 625.0f) / areaInSqMeters,
            _ => throw new ArgumentOutOfRangeException(nameof(units))
        };
    }

    // ULocalLightComponent::GetUnitsConversionFactor
    public static float GetUnitsConversionFactor(ELightUnits srcUnits, ELightUnits targetUnits, float cosHalfConeAngle)
    {
        if (srcUnits == targetUnits) return 1f;

        cosHalfConeAngle = Math.Clamp(cosHalfConeAngle, -1, 1 - UnrealMath.KindaSmallNumber);
        float factor = GetSourceUnitsFactor(srcUnits, cosHalfConeAngle);
        return factor * GetTargetUnitsModifier(targetUnits, cosHalfConeAngle);
    }

    private static float GetSourceUnitsFactor(ELightUnits units, float cosHalfConeAngle) => units switch {
        ELightUnits.Candelas => 100f * 100f,
        ELightUnits.Lumens => (float)(100f * 100f / 2f / Math.PI / (1f - cosHalfConeAngle)),
        ELightUnits.EV => 100f * 100f,
        ELightUnits.Nits => 1f,
        _ => 16f
    };

    private static float GetTargetUnitsModifier(ELightUnits units, float cosHalfConeAngle) => units switch {
        ELightUnits.Candelas => 1f / 100f / 100f,
        ELightUnits.Lumens => (float)(2f * Math.PI * (1f - cosHalfConeAngle) / 100f / 100f),
        ELightUnits.EV => 1f / 100f / 100f,
        ELightUnits.Nits => 1f,
        _ => 1f / 16f
    };
}
