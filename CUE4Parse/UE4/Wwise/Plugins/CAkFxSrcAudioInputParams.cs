using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkFxSrcAudioInputParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkFXSrcAudioInputParams Params = Ar.Read<AkFXSrcAudioInputParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkFXSrcAudioInputParams
{
    public float fGain;
};
