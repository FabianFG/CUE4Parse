using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkCompressorFXParams(FArchive Ar) : IAkPluginParam
{
    public AkCompressorFXParams Params = new AkCompressorFXParams(Ar);
}

public struct AkCompressorFXParams(FArchive Ar)
{
    public float fThreshold = Ar.Read<float>();
    public float fRatio = Ar.Read<float>();
    public float fAttack = Ar.Read<float>();
    public float fRelease = Ar.Read<float>();
    public float fOutputLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public bool bProcessLFE = Ar.Read<byte>() != 0;
    public bool bChannelLink = Ar.Read<byte>() != 0;
}
