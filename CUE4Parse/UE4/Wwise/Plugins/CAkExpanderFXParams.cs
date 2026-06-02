using System;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkExpanderFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkExpanderParams Params = new(Ar);
}

public struct AkExpanderParams(FWwiseArchive Ar)
{
    public float fThreshold = Ar.Read<float>();
    public float fRatio = Ar.Read<float>();
    public float fAttack = Ar.Read<float>();
    public float fRelease = Ar.Read<float>();
    public float fOutputLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
    public bool bProcessLFE = Ar.Read<byte>() != 0;
    public bool bChannelLink = Ar.Read<byte>() != 0;
}

