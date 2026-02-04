using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFxSrcSineParams(FArchive Ar) : IAkPluginParam
{
    public AkFxSrcSineParams Params = new AkFxSrcSineParams(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkFxSrcSineParams(FArchive Ar)
{
    public float fFrequency = Ar.Read<float>();
    public float fGain = (float) System.Math.Pow(10f, Ar.Read<float>() * 0.05);
    public float fDuration = Ar.Read<float>();
    public uint uChannelMask = Ar.Read<uint>();
}
