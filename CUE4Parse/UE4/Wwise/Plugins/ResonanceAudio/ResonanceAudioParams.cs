namespace CUE4Parse.UE4.Wwise.Plugins.ResonanceAudio;

public class ResonanceAudioParams(FWwiseArchive Ar) : IAkPluginParam
{
    public bool Bypass = Ar.Read<byte>() != 0;
}
