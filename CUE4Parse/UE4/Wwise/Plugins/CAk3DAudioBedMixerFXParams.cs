using System.Runtime.InteropServices;
using CUE4Parse.UE4.Wwise.Enums;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAk3DAudioBedMixerFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public Ak3DAudioBedMixerRTPCParams Params = new(Ar);
}

[StructLayout(LayoutKind.Sequential)]
public struct Ak3DAudioBedMixerRTPCParams(FWwiseArchive Ar)
{
    public EAkChannelConfig MainMixConfiguration = Ar.Read<EAkChannelConfig>();
    public ushort PassthroughMixPolicy = Ar.Read<ushort>();
    public ushort SystemAudioObjectsPolicy = Ar.Read<ushort>();
    public ushort SystemAudioObjectLimit = Ar.Read<ushort>();
}
