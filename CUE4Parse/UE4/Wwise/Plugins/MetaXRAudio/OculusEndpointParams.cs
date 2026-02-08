using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.MetaXRAudio;

public class OculusEndpointSinkParams(FArchive Ar) : IAkPluginParam
{
    public ushort SpatializedVoiceLimit = Ar.Read<ushort>();
    public float GlobalScale = Ar.Read<float>();
    public float RoomLength = Ar.Read<float>();
    public float RoomWidth = Ar.Read<float>();
    public float RoomHeight = Ar.Read<float>();
    public ushort LeftWallMaterial = Ar.Read<ushort>();
    public ushort RightWallMaterial = Ar.Read<ushort>();
    public ushort FrontWallMaterial = Ar.Read<ushort>();
    public ushort BackWallMaterial = Ar.Read<ushort>();
    public ushort CeilingMaterial = Ar.Read<ushort>();
    public ushort FloorMaterial = Ar.Read<ushort>();
    public bool EnableEarlyReflections = Ar.Read<byte>() != 0;
    public bool EnableReverb = Ar.Read<byte>() != 0;
    public float ReverbWetLevel = Ar.Read<float>();
    public float ClutterFactor = Ar.Read<float>();
}

public class OculusEndpointMetadataParams(FArchive Ar) : IAkPluginParam
{
    public bool EnableAcoustics = Ar.Read<byte>() != 0;
    public float ReverbSendLevel = Ar.Read<float>();
    public ushort DistanceAttenuationMode = Ar.Read<ushort>();
}

public class OculusEndpointExperimentalMetadataParams(FArchive Ar) : IAkPluginParam
{
    public ushort DirectivityPattern = Ar.Read<ushort>();
    public float ReflectionSendLevel = Ar.Read<float>();
    public float VolumetricRadius = Ar.Read<float>();
    public float HRTFIntensity = Ar.Read<float>();
    public bool SoloReverbSend = Ar.Read<byte>() != 0;
    public float DirectivityIntensity = Ar.Read<float>();
    public float ReverbReach = Ar.Read<float>();
    public float OcclusionIntensity = Ar.Read<float>();
    public bool MediumAbsorption = Ar.Read<byte>() != 0;
}
