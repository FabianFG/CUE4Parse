using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins.McDSP;

public class CMcDSPLimiterFXParams(FArchive Ar) : IAkPluginParam
{
    public McDSPLimiterFXParams Params = Ar.Read<McDSPLimiterFXParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct McDSPLimiterFXParams
{
    public float fCeiling;
    public float fThreshold;
    public float fKnee;
    public float fRelease;
    public LimiterCharacterType eMode;
};

[JsonConverter(typeof(StringEnumConverter))]
public enum LimiterCharacterType : int
{
    eCharacterMode_Clean = 0x0,
    eCharacterMode_Soft = 0x1,
    eCharacterMode_Smart = 0x2,
    eCharacterMode_Dynamic = 0x3,
    eCharacterMode_Loud = 0x4,
    eCharacterMode_Crush = 0x5
};
