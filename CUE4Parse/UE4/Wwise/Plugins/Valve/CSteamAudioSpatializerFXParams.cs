using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Wwise.Plugins.Valve;

public struct CSteamAudioSpatializerFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public short Occlusion = Ar.Read<short>();
    public float OcclusionValue = Ar.Read<float>();
    public short Transmission = Ar.Read<short>();
    public EIPLTransmissionType TransmissionType = Ar.Read<EIPLTransmissionType>();
    public FVector TransmissionValue = Ar.Read<FVector>();
    public bool DirectBinaural = Ar.ReadBool();
    public FVector Pos = Ar.Read<FVector>();
    public short HrtfInterpolation = Ar.Read<short>();
    public bool DistanceAttenuation = Ar.ReadBool();
    public bool AirAbsorption = Ar.ReadBool();
    public bool Directivity = Ar.ReadBool();
    public float DipoleWeight = Ar.Read<float>();
    public float DipolePower = Ar.Read<float>();
    public float DirectMixLevel = Ar.Read<float>();
    public bool Reflections = Ar.ReadBool();
    public bool ReflectionsBinaural = Ar.ReadBool();
    public float ReflectionsMixLevel = Ar.Read<float>();
    public bool Pathing = Ar.ReadBool();
    public bool PathingBinaural = Ar.ReadBool();
    public float PathingMixLevel = Ar.Read<float>();
    public bool PathingNormalizeEQ = Ar.ReadBool();

    public enum EIPLTransmissionType : short
    {
        /** Transmission is frequency-independent. */
        IPL_TRANSMISSIONTYPE_FREQINDEPENDENT,

        /** Transmission is frequency-dependent. */
        IPL_TRANSMISSIONTYPE_FREQDEPENDENT
    }
}

public struct CSteamAudioReverbFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public bool Binaural = Ar.ReadBool();
}

