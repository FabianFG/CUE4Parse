using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UExponentialHeightFogComponent : USceneComponent
{
    public float FogDensity { get; private set; } = 0.02f;
    public float FogHeightFalloff { get; private set; } = 0.2f;
    public float FogMaxOpacity { get; private set; } = 1.0f;
    public float StartDistance { get; private set; } = 0.0f;
    public FLinearColor FogInscatteringLuminance { get; private set; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    public FLinearColor DirectionalInscatteringLuminance { get; private set; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    public float DirectionalInscatteringExponent { get; private set; } = 4.0f;
    public float DirectionalInscatteringStartDistance { get; private set; } = 10000.0f;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        FogDensity = GetOrDefault(nameof(FogDensity), FogDensity);
        FogHeightFalloff = GetOrDefault(nameof(FogHeightFalloff), FogHeightFalloff);
        FogMaxOpacity = GetOrDefault(nameof(FogMaxOpacity), FogMaxOpacity);
        StartDistance = GetOrDefault(nameof(StartDistance), StartDistance);
        FogInscatteringLuminance = GetOrDefault(nameof(FogInscatteringLuminance), FogInscatteringLuminance);
        DirectionalInscatteringLuminance = GetOrDefault(nameof(DirectionalInscatteringLuminance), DirectionalInscatteringLuminance);
        DirectionalInscatteringExponent = GetOrDefault(nameof(DirectionalInscatteringExponent), DirectionalInscatteringExponent);
        DirectionalInscatteringStartDistance = GetOrDefault(nameof(DirectionalInscatteringStartDistance), DirectionalInscatteringStartDistance);

        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.SkyAtmosphereAffectsHeightFogWithBetterDefault)
        {
            FogInscatteringLuminance = GetOrDefault("FogInscatteringColor", new FLinearColor(0.447f, 0.638f, 1.0f, 1.0f));
            DirectionalInscatteringLuminance = GetOrDefault("DirectionalInscatteringColor", new FLinearColor(0.25f, 0.25f, 0.125f, 1.0f));
        }
    }
}
