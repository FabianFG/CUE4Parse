using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.ResonanceAudio;

public class ResonanceAudioParams(FArchive Ar) : IAkPluginParam
{
    public bool Bypass = Ar.Read<byte>() != 0;
}
