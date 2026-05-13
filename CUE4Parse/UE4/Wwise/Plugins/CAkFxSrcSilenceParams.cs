using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFxSrcSilenceParams(FWwiseArchive Ar) : IAkPluginParam
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
