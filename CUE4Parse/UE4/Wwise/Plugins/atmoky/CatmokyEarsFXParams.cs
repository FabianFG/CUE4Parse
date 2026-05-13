namespace CUE4Parse.UE4.Wwise.Plugins.atmoky;

public class CAtmokyEarsFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public float ExternalizerAmount = Ar.Read<float>();
    public float PersonalizationSize = Ar.Read<float>();
    public int ExternalizerCharacter = Ar.Read<int>();
    public byte PerformanceMode = Ar.Read<byte>();
}
