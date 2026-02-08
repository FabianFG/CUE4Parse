using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFxSrcSilenceParams(FArchive Ar) : IAkPluginParam
{
    public AkFxSrcSilenceParams Params = Ar.Read<AkFxSrcSilenceParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkFxSrcSilenceParams
{
    public float fDuration;
    public float fRandomizedLengthMinus;
    public float fRandomizedLengthPlus;
}
