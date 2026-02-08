using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkDefaultSinkParams : IAkPluginParam;

public class CAkSystemSinkParams(FArchive Ar) : IAkPluginParam
{
    public bool Allow3DAudio = Ar.Read<byte>() != 0;
    public uint MainMixHeadphoneConfiguration = Ar.Read<uint>();
    public uint MainMixSpeakerConfiguration = Ar.Read<uint>();
    public bool AllowSystemAudioObjects = Ar.Read<byte>() != 0;
    public ushort MinSystemAudioObjectsRequired = Ar.Read<ushort>();
}

public class CAkDVRSinkParams(FArchive Ar) : IAkPluginParam
{
    public bool DVRRecordable = Ar.Read<byte>() != 0;
}

