using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkFxSrcAudioInputParams(FArchive Ar) : IAkPluginParam
{
    public AkFXSrcAudioInputParams Params = Ar.Read<AkFXSrcAudioInputParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkFXSrcAudioInputParams
{
    public float fGain;
};
