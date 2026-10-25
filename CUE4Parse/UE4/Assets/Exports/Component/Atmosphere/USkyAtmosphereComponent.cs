using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.Atmosphere;

public enum ESkyAtmosphereTransformMode : byte
{
    PlanetTopAtAbsoluteWorldOrigin,
    PlanetTopAtComponentTransform,
    PlanetCenterAtComponentTransform
}

public class USkyAtmosphereComponent : USceneComponent
{
    private const float EarthBottomRadius = 6360.0f;
    private const float EarthTopRadius = 6420.0f;
    private const float EarthRayleighScaleHeight = 8.0f;
    private const float EarthMieScaleHeight = 1.2f;

    public ESkyAtmosphereTransformMode TransformMode { get; private set; } = ESkyAtmosphereTransformMode.PlanetTopAtAbsoluteWorldOrigin;
    public float BottomRadius { get; private set; } = EarthBottomRadius;
    public FColor GroundAlbedo { get; private set; } = new(170, 170, 170, 255);
    public float AtmosphereHeight { get; private set; } = EarthTopRadius - EarthBottomRadius;
    public float RayleighScatteringScale { get; private set; } = 0.0331f;
    public FLinearColor RayleighScattering { get; private set; } = new(0.175287f, 0.409607f, 1.0f, 1.0f);
    public float RayleighExponentialDistribution { get; private set; } = EarthRayleighScaleHeight;
    public float MieScatteringScale { get; private set; } = 0.003996f;
    public FLinearColor MieScattering { get; private set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public float MieAbsorptionScale { get; private set; } = 0.000444f;
    public FLinearColor MieAbsorption { get; private set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public float MieAnisotropy { get; private set; } = 0.8f;
    public float MieExponentialDistribution { get; private set; } = EarthMieScaleHeight;
    public float OtherAbsorptionScale { get; private set; } = 0.001881f;
    public FLinearColor OtherAbsorption { get; private set; } = new(0.3454f, 1.0f, 0.0452f, 1.0f);
    public FLinearColor SkyLuminanceFactor { get; private set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public FGuid bStaticLightingBuiltGUID;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        TransformMode = GetOrDefault(nameof(TransformMode), TransformMode);
        BottomRadius = GetOrDefault(nameof(BottomRadius), BottomRadius);
        GroundAlbedo = GetOrDefault(nameof(GroundAlbedo), GroundAlbedo);
        AtmosphereHeight = GetOrDefault(nameof(AtmosphereHeight), AtmosphereHeight);
        RayleighScatteringScale = GetOrDefault(nameof(RayleighScatteringScale), RayleighScatteringScale);
        RayleighScattering = GetOrDefault(nameof(RayleighScattering), RayleighScattering);
        RayleighExponentialDistribution = GetOrDefault(nameof(RayleighExponentialDistribution), RayleighExponentialDistribution);
        MieScatteringScale = GetOrDefault(nameof(MieScatteringScale), MieScatteringScale);
        MieScattering = GetOrDefault(nameof(MieScattering), MieScattering);
        MieAbsorptionScale = GetOrDefault(nameof(MieAbsorptionScale), MieAbsorptionScale);
        MieAbsorption = GetOrDefault(nameof(MieAbsorption), MieAbsorption);
        MieAnisotropy = GetOrDefault(nameof(MieAnisotropy), MieAnisotropy);
        MieExponentialDistribution = GetOrDefault(nameof(MieExponentialDistribution), MieExponentialDistribution);
        OtherAbsorptionScale = GetOrDefault(nameof(OtherAbsorptionScale), OtherAbsorptionScale);
        OtherAbsorption = GetOrDefault(nameof(OtherAbsorption), OtherAbsorption);
        SkyLuminanceFactor = GetOrDefault(nameof(SkyLuminanceFactor), SkyLuminanceFactor);

        var bIsAtmosphericFog = this is UAtmosphericFogComponent;
        if ((FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.RemovedAtmosphericFog && bIsAtmosphericFog) || !bIsAtmosphericFog)
            bStaticLightingBuiltGUID = Ar.Read<FGuid>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("bStaticLightingBuiltGUID");
        writer.WriteValue(bStaticLightingBuiltGUID.ToString(EGuidFormats.UniqueObjectGuid));
    }
}
