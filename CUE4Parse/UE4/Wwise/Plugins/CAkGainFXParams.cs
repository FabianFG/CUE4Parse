using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkGainFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkGainFXParams Params = Ar.Read<AkGainFXParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkGainFXParams
{
    public float fFullbandGain;
    public float fLFEGain;
};
