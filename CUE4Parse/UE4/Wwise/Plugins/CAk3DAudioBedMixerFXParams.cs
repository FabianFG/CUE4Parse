using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAk3DAudioBedMixerFXParams(FArchive Ar) : IAkPluginParam
{
    public Ak3DAudioBedMixerFXParams Params = Ar.Read<Ak3DAudioBedMixerFXParams>();
}

[StructLayout(LayoutKind.Sequential)]
public struct Ak3DAudioBedMixerFXParams
{
    public uint unknown1;
    public ushort unknown2;
    public ushort unknown3;
    public uint unknown4;
}
