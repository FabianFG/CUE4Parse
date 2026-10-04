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
    public float Intensity { get; protected set; } = MathF.PI;
    public FColor LightColor { get; private set; } = new(255, 255, 255, 255);
    public bool CastShadows { get; private set; } = true;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Intensity = GetOrDefault(nameof(Intensity), Intensity);
        LightColor = GetOrDefault(nameof(LightColor), LightColor);
        CastShadows = GetOrDefault(nameof(CastShadows), CastShadows);

        if (Ar.Ver < EUnrealEngineObjectUE4Version.INVERSE_SQUARED_LIGHTS_DEFAULT)
        {
            Intensity = GetOrDefault("Brightness", MathF.PI);
        }
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
    public bool bUseTemperature { get; private set; }
    public FPackageIndex IESTexture { get; private set; }
    public bool bUseIESBrightness { get; private set; }
    public float IESBrightnessScale { get; private set; }
    public FStaticShadowDepthMapData? LegacyData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Temperature = GetOrDefault(nameof(Temperature), 6500.0f);
        MaxDrawDistance = GetOrDefault(nameof(MaxDrawDistance), 0.0f);
        MaxDistanceFadeRange = GetOrDefault(nameof(MaxDistanceFadeRange), 0.0f);
        bUseTemperature = GetOrDefault(nameof(bUseTemperature), false);
        IESTexture = GetOrDefault(nameof(IESTexture), new FPackageIndex());
        bUseIESBrightness = GetOrDefault(nameof(bUseIESBrightness), false);
        IESBrightnessScale = GetOrDefault(nameof(IESBrightnessScale), 1.0f);

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.STATIC_SHADOW_DEPTH_MAPS)
        {
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
            {
                LegacyData = new FStaticShadowDepthMapData(Ar);
            }
        }

        /*if (Ar.Ver > EUnrealEngineObjectUE3Version.ADDED_LIGHT_VOLUME_SUPPORT && Ar.Ver < EUnrealEngineObjectUE3Version.REMOVE_UNUSED_LIGHTING_PROPERTIES)
        {
            Ar.ReadArray(() => new FConvexVolume(Ar)); // InclusionConvexVolumes
            Ar.ReadArray(() => new FConvexVolume(Ar)); // ExclusionConvexVolumes
        }*/

        if (Ar.Game == GAME_Valorant) Ar.Position += 24; // Zero FVector, 1.0f, -1 int, 1.0f
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

        if (Ar.Game is GAME_LordOfMysteries) Ar.Position += 24;
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


public class UDominantSpotLightComponent : UPointLightComponent
{
    public short[] DominantLightShadowMap;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        // Before super
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.SPOTLIGHT_DOMINANTSHADOW_TRANSITION && Ar.Game < GAME_UE4_0)
        {
            DominantLightShadowMap = Ar.ReadArray<short>();
        }

        base.Deserialize(Ar, validPos);
    }
}

public class UDominantDirectionalLightComponent : UPointLightComponent
{
    public short[]? DominantLightShadowMap;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        // Before super
        if (Ar.Ver >= EUnrealEngineObjectUE3Version.DOMINANTLIGHT_NORMALSHADOWS && Ar.Game < GAME_UE4_0)
        {
            DominantLightShadowMap = Ar.ReadArray<short>();
        }

        base.Deserialize(Ar, validPos);
    }
}
public class UDominantPointLightComponent : UPointLightComponent;

public class ULightEnvironmentComponent : UActorComponent;

public class UParticleLightEnvironmentComponent : UPointLightComponent;
public class UDynamicLightEnvironmentComponent : ULightEnvironmentComponent;

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
    public float LightSourceAngle { get; private set; } = 0.5357f;
    public float LightSourceSoftAngle { get; private set; }
    public bool bAtmosphereSunLight { get; private set; } = true;
    public int AtmosphereSunLightIndex { get; private set; }

    public UDirectionalLightComponent()
    {
        Intensity = 10.0f; // kill PI from ULightComponentBase
    }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LightSourceAngle = GetOrDefault(nameof(LightSourceAngle), LightSourceAngle);
        LightSourceSoftAngle = GetOrDefault(nameof(LightSourceSoftAngle), LightSourceSoftAngle);
        bAtmosphereSunLight = GetOrDefault(nameof(bAtmosphereSunLight), bAtmosphereSunLight);
        AtmosphereSunLightIndex = GetOrDefault(nameof(AtmosphereSunLightIndex), AtmosphereSunLightIndex);

        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.DirLightsAreAtmosphereLightsByDefault)
        {
            bAtmosphereSunLight = GetOrDefault("bUsedAsAtmosphereSunLight", false);
        }
    }
}

public class USkyLightComponent : ULightComponentBase
{
    public bool bLowerHemisphereIsBlack { get; private set; } = true;
    public FLinearColor LowerHemisphereColor { get; private set; } = new(0.0f, 0.0f, 0.0f, 1.0f);

    public USkyLightComponent()
    {
        Intensity = 1.0f; // kill PI from ULightComponentBase
    }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bLowerHemisphereIsBlack = GetOrDefault(nameof(bLowerHemisphereIsBlack), bLowerHemisphereIsBlack);
        LowerHemisphereColor = GetOrDefault(nameof(LowerHemisphereColor), LowerHemisphereColor);

        if (Ar.Ver >= EUnrealEngineObjectUE4Version.SKYLIGHT_MOBILE_IRRADIANCE_MAP && !(FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.SkyLightRemoveMobileIrradianceMap))
        {
            // DummyIrradianceEnvironmentMap
        }
    }
}
