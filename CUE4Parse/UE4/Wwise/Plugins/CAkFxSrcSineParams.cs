using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFxSrcSineParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkFxSrcSineParams Params = new(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkFxSrcSineParams(FWwiseArchive Ar)
{
    public float fFrequency = Ar.Read<float>();
    public float fGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
    public float fDuration = Ar.Read<float>();
    public uint uChannelMask = Ar.Read<uint>();
}
