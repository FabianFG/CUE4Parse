using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAk3DAudioBedMixerFXParams(FArchive Ar) : IAkPluginParam
{
    public Ak3DAudioBedMixerRTPCParams Params = new Ak3DAudioBedMixerRTPCParams(Ar);
}

[StructLayout(LayoutKind.Sequential)]
public struct Ak3DAudioBedMixerRTPCParams(FArchive Ar)
{
    public AkChannelConfig MainMixConfiguration = Ar.Read<AkChannelConfig>();
    public ushort PassthroughMixPolicy = Ar.Read<ushort>();
    public ushort SystemAudioObjectsPolicy = Ar.Read<ushort>();
    public ushort SystemAudioObjectLimit = Ar.Read<ushort>();
}
