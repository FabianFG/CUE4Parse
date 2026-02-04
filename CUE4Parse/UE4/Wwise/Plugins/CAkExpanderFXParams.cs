using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkExpanderFXParams(FArchive Ar) : IAkPluginParam
{
    public AkExpanderParams Params = new AkExpanderParams(Ar);
}

public struct AkExpanderParams(FArchive Ar)
{
    public float fThreshold = Ar.Read<float>();
    public float fRatio = Ar.Read<float>();
    public float fAttack = Ar.Read<float>();
    public float fRelease = Ar.Read<float>();
    public float fOutputLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    public bool bProcessLFE = Ar.Read<byte>() != 0;
    public bool bChannelLink = Ar.Read<byte>() != 0;
}

