using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.OculusSpatializer;

public class COculusSpatializerFXAttachmentParams(FArchive Ar) : IAkPluginParam
{
    public bool BypassSpatializer = Ar.Read<byte>() != 0;
    public bool EnableReflections = Ar.Read<byte>() != 0;
    public bool UseInvSqAttenuation = Ar.Read<byte>() != 0;
    public float AttenuationRangeMin = Ar.Read<float>();
    public float AttenuationRangeMax = Ar.Read<float>();
    public float ReverbSendLevel = Ar.Read<float>();
    public float VolumetricRadius = Ar.Read<float>();
    public bool Ambisonic = Ar.Read<byte>() != 0;
}

public class COculusSpatializerFXParams(FArchive Ar) : IAkPluginParam
{
    public float Version = Ar.Read<float>();
    public bool Bypass = Ar.Read<byte>() != 0;
    public bool EnableReflections = Ar.Read<byte>() != 0;
    public float RoomSizeX = Ar.Read<float>();
    public float RoomSizeY = Ar.Read<float>();
    public float RoomSizeZ = Ar.Read<float>();
    public float ReflectLeft = Ar.Read<float>();
    public float ReflectRight = Ar.Read<float>();
    public float ReflectFront = Ar.Read<float>();
    public float ReflectBehind = Ar.Read<float>();
    public float ReflectUp = Ar.Read<float>();
    public float ReflectDown = Ar.Read<float>();
    public float GlobalScale = Ar.Read<float>();
    public float Gain = Ar.Read<float>();
    public bool DEBUG_ClampPos = Ar.Read<byte>() != 0;
    public bool DEBUG_Misc = Ar.Read<byte>() != 0;
    public bool ReverbOn = Ar.Read<byte>() != 0;
    public float ReflectionsRangeMin = Ar.Read<float>();
    public float ReflectionsRangeMax = Ar.Read<float>();
    public float ReverbWetMix = Ar.Read<float>();
    public int VoiceLimit = Ar.Read<int>();
}
