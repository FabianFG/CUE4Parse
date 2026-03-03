using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.atmoky;

public class CAtmokyEarsFXParams(FArchive Ar) : IAkPluginParam
{
    public float ExternalizerAmount = Ar.Read<float>();
    public float PersonalizationSize = Ar.Read<float>();
    public int ExternalizerCharacter = Ar.Read<int>();
    public byte PerformanceMode = Ar.Read<byte>();
}
