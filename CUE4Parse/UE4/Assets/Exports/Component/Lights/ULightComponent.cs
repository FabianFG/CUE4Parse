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

    public virtual float GetNitIntensity() => Intensity;
}

public class ULightComponent : ULightComponentBase
{
    public float Temperature { get; private set; }
    public float MaxDrawDistance { get; private set; }
    public float MaxDistanceFadeRange { get; private set; }
    public uint bUseTemperature { get; private set; }
    public FPackageIndex IESTexture { get; private set; }
    public uint bUseIESBrightness { get; private set; }
    public float IESBrightnessScale { get; private set; }
    public FStaticShadowDepthMapData? LegacyData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Temperature = GetOrDefault(nameof(Temperature), 6500.0f);
        MaxDrawDistance = GetOrDefault(nameof(MaxDrawDistance), 0.0f);
        MaxDistanceFadeRange = GetOrDefault(nameof(MaxDistanceFadeRange), 0.0f);
        bUseTemperature = GetOrDefault(nameof(bUseTemperature), 0u);
        IESTexture = GetOrDefault(nameof(IESTexture), new FPackageIndex());
        bUseIESBrightness = GetOrDefault(nameof(bUseIESBrightness), 0u);
        IESBrightnessScale = GetOrDefault(nameof(IESBrightnessScale), 1.0f);

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_SHADOW_DEPTH_MAPS)
        {
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
            {
                LegacyData = new FStaticShadowDepthMapData(Ar);
            }
        }

        if (Ar.Game == EGame.GAME_Valorant) Ar.Position += 24; // Zero FVector, 1.0f, -1 int, 1.0f
    }

    public virtual ELightUnits GetLightUnits() => ELightUnits.Unitless;

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

    public override ELightUnits GetLightUnits() => IntensityUnits;

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

    public override float GetNitIntensity()
    {
        if (!bUseInverseSquaredFalloff)
            return Intensity; // Unitless brightness

        var solidAngle = 4f * MathF.PI;
        if (this is USpotLightComponent spotLightComponent)
            solidAngle = 2f * MathF.PI * (1.0f - spotLightComponent.GetCosHalfConeAngle());

        var areaInSqMeters = (float)Math.Max(solidAngle * Math.Pow(SourceRadius / 100f, 2), UnrealMath.KindaSmallNumber);

        float intensity = Intensity;
        if (UnrealMath.IsNearlyZero(SourceRadius))
        {
            intensity = 0.0f;
        }

        return LightUtils.ConvertToIntensityToNits(intensity, areaInSqMeters, solidAngle, IntensityUnits);
    }
}

public class URectLightComponent : ULocalLightComponent
{
    public float SourceWidth { get; private set; }
    public float SourceHeight { get; private set; }
    public float BarnDoorAngle { get; private set; }
    public float BarnDoorLength { get; private set; }
    public float LightFunctionConeAngle { get; private set; }
    public FPackageIndex SourceTexture { get; private set; }
    public FVector2D SourceTextureScale { get; private set; }
    public FVector2D SourceTextureOffset { get; private set; }
    public bool bLightRequiresBrokenEVMath { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SourceWidth = GetOrDefault(nameof(SourceWidth), 64.0f);
        SourceHeight = GetOrDefault(nameof(SourceHeight), 64.0f);
        BarnDoorAngle = GetOrDefault(nameof(BarnDoorAngle), 88.0f);
        BarnDoorLength = GetOrDefault(nameof(BarnDoorLength), 20.0f);
        LightFunctionConeAngle = GetOrDefault(nameof(LightFunctionConeAngle), 0.0f);

        SourceTexture = GetOrDefault(nameof(SourceTexture), new FPackageIndex());
        SourceTextureScale = GetOrDefault(nameof(SourceTextureScale), new FVector2D(1.0f, 1.0f));
        SourceTextureOffset = GetOrDefault(nameof(SourceTextureOffset), new FVector2D(0.0f, 0.0f));

        if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.RectLightFixedEVUnitConversion)
        {
            if (IntensityUnits == ELightUnits.EV)
            {
                bLightRequiresBrokenEVMath = true;
            }
        }
    }

    public override float GetNitIntensity()
    {
        var areaInSqMeters = (SourceWidth / 100.0f) * (SourceHeight / 100.0f);

        float intensity = Intensity;
        if (UnrealMath.IsNearlyZero(areaInSqMeters))
        {
            intensity = 0.0f;
        }

        return LightUtils.ConvertToIntensityToNits(intensity, areaInSqMeters, MathF.PI, IntensityUnits);
    }
}

public class UDirectionalLightComponent : ULightComponent
{
    public float LightSourceAngle { get; private set; }
    public float LightSourceSoftAngle { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LightSourceAngle = GetOrDefault(nameof(LightSourceAngle), 0.5357f);
        LightSourceSoftAngle = GetOrDefault(nameof(LightSourceSoftAngle), 0.0f);
    }
}

public class USkyLightComponent : ULightComponentBase;
