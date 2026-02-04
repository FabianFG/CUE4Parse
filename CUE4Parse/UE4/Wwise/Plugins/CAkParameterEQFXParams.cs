using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkParameterEQFXParams(FArchive Ar) : IAkPluginParam
{
    public AkParametricEQFXParams Params = new AkParametricEQFXParams(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EQModuleParams
{
    public AkFilterType eFilterType;
    public float fGain;
    public float fFrequency;
    public float fQFactor;
    public bool bOnOff;
}

public struct AkParametricEQFXParams
{
    public EQModuleParams[] Band;
    public float fOutputLevel;
    public bool bProcessLFE;

    public AkParametricEQFXParams(FArchive Ar)
    {
        Band = Ar.ReadArray<EQModuleParams>(3);
        fOutputLevel = Ar.Read<float>();
        bProcessLFE = Ar.Read<byte>() != 0;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkFilterType : uint
{
    LowShelf = 0x0,
    PeakingEQ = 0x1,
    HighShelf = 0x2,
    LowPass = 0x3,
    HighPass = 0x4,
    BandPass = 0x5,
    Notch = 0x6
}
