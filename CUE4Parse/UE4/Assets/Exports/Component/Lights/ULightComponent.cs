using System;
using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.Lights;

public class ULightComponentBase : USceneComponent
{
    public float Intensity { get; protected set; }
    public FColor LightColor { get; private set; }
    public uint CastShadows { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Intensity = GetOrDefault(nameof(Intensity), GetOrDefault("Brightness", MathF.PI));
        LightColor = GetOrDefault(nameof(LightColor), new FColor(255, 255, 255, 255));
        CastShadows = GetOrDefault(nameof(CastShadows), 1u);
    }

    public FLinearColor GetLightColor()
    {
        return new FLinearColor(LightColor.R / 255.0f, LightColor.G / 255.0f, LightColor.B / 255.0f, LightColor.A / 255.0f);
    }
}

public class ULightComponent : ULightComponentBase
{
    public float Temperature { get; private set; }
    public float MaxDrawDistance { get; private set; }
    public float MaxDistanceFadeRange { get; private set; }
    public uint bUseTemperature { get; private set; }
    public FPackageIndex IESTexture { get; private set; }
    public FStaticShadowDepthMapData? LegacyData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Temperature = GetOrDefault(nameof(Temperature), 6500.0f);
        MaxDrawDistance = GetOrDefault(nameof(MaxDrawDistance), 0.0f);
        MaxDistanceFadeRange = GetOrDefault(nameof(MaxDistanceFadeRange), 0.0f);
        bUseTemperature = GetOrDefault(nameof(bUseTemperature), 0u);
        IESTexture = GetOrDefault(nameof(IESTexture), new FPackageIndex());

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_SHADOW_DEPTH_MAPS)
        {
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
            {
                LegacyData = new FStaticShadowDepthMapData(Ar);
            }
        }

        if (Ar.Game == EGame.GAME_Valorant) Ar.Position += 24; // Zero FVector, 1.0f, -1 int, 1.0f
    }

    public virtual double GetNitIntensity() => Intensity;

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (LegacyData != null)
        {
            writer.WritePropertyName("LegacyData");
            serializer.Serialize(writer, LegacyData);
        }
    }
}

public class ULocalLightComponent : ULightComponent
{
    public float AttenuationRadius;
    public ELightUnits IntensityUnits;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AttenuationRadius = GetOrDefault(nameof(AttenuationRadius), 1000.0f);
        IntensityUnits = GetOrDefault(nameof(IntensityUnits), Owner.Provider.DefaultLightUnit);
    }

    public override double GetNitIntensity()
    {
        return Intensity;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("IntensityNits");
        serializer.Serialize(writer, GetNitIntensity());
    }
}

public class USpotLightComponent : UPointLightComponent
{
    public float InnerConeAngle { get; private set; }
    public float OuterConeAngle { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        InnerConeAngle = GetOrDefault(nameof(InnerConeAngle), 0.0f);
        OuterConeAngle = GetOrDefault(nameof(OuterConeAngle), 44.0f);
    }

    private float GetHalfConeAngle()
    {
        var clampedInnerConeAngle = Math.Clamp(InnerConeAngle, 0.0f, 89.0f) * MathF.PI / 180.0f;
        var clampedOuterConeAngle = Math.Clamp(OuterConeAngle * MathF.PI / 180.0f, clampedInnerConeAngle + 0.001f,
            89.0f * MathF.PI / 180.0f + 0.001f);
        return clampedOuterConeAngle;
    }

    public float GetCosHalfConeAngle()
    {
        return MathF.Cos(GetHalfConeAngle());
    }
}

public class UPointLightComponent : ULocalLightComponent
{
    public float LightFalloffExponent { get; private set; }
    public float SourceRadius { get; private set; }
    public float SoftSourceRadius { get; private set; }
    public float SourceLength { get; private set; }
    public bool bUseInverseSquaredFalloff { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LightFalloffExponent = GetOrDefault(nameof(LightFalloffExponent), 8.0f);
        SourceRadius = GetOrDefault(nameof(SourceRadius), 0.0f);
        SoftSourceRadius = GetOrDefault(nameof(SoftSourceRadius), 0.0f);
        SourceLength = GetOrDefault(nameof(SourceLength), 0.0f);
        bUseInverseSquaredFalloff = GetOrDefault(nameof(bUseInverseSquaredFalloff), GetOrDefault("InverseSquaredFalloff", true));

        if (Ar.Ver < EUnrealEngineObjectUE4Version.POINTLIGHT_SOURCE_ORIENTATION && SourceLength > UnrealMath.KindaSmallNumber && IESTexture.IsNull)
        {
            AddLocalRotation(new FRotator(-90.0f, 0.0f, 0.0f));
        }

        if (!bUseInverseSquaredFalloff)
        {
            IntensityUnits = ELightUnits.Unitless;
        }
    }

    public override double GetNitIntensity()
    {
        if (!bUseInverseSquaredFalloff)
            return Intensity; // Unitless brightness

        double solidAngle = 4f * Math.PI;
        if (this is USpotLightComponent spotLightComponent)
            solidAngle = 2f * Math.PI * (1.0f - spotLightComponent.GetCosHalfConeAngle());

        float areaInSqMeters =
            (float)Math.Max(solidAngle * Math.Pow(SourceRadius / 100f, 2), UnrealMath.KindaSmallNumber);

        return LightUtils.ConvertToIntensityToNits(Intensity, areaInSqMeters, solidAngle, IntensityUnits);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("bUseInverseSquaredFalloff");
        writer.WriteValue(bUseInverseSquaredFalloff);
    }
}

public class URectLightComponent : ULocalLightComponent
{
    public float SourceWidth;
    public float SourceHeight;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SourceWidth = GetOrDefault(nameof(SourceWidth), 64.0f);
        SourceHeight = GetOrDefault(nameof(SourceHeight), 64.0f);
    }

    public override double GetNitIntensity()
    {
        var areaInSqMeters = (SourceWidth / 100.0f) * (SourceHeight / 100.0f);
        const double angle = Math.PI;
        return LightUtils.ConvertToIntensityToNits(Intensity, areaInSqMeters, angle, IntensityUnits);
    }
}

public class UDirectionalLightComponent : ULightComponent
{
    public override double GetNitIntensity() {
        throw new NotImplementedException();
    }
}

public class USkyLightComponent : ULightComponentBase
{

}
