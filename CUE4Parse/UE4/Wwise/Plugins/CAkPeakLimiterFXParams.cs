using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkPeakLimiterFXParams(FArchive Ar) : IAkPluginParam
{
    public AkPeakLimiterFXParams Params = new AkPeakLimiterFXParams(Ar);
}

public struct AkPeakLimiterFXParams
{
    public AkPeakLimiterRTPCParams RTPC;
    public AkPeakLimiterNonRTPCParams NonRTPC;

    public AkPeakLimiterFXParams(FArchive Ar)
    {
        RTPC.fThreshold = Ar.Read<float>();
        RTPC.fRatio = Ar.Read<float>();
        NonRTPC.fLookAhead = Ar.Read<float>();
        RTPC.fRelease = Ar.Read<float>();
        RTPC.fOutputLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        NonRTPC.bProcessLFE = Ar.Read<byte>() != 0;
        NonRTPC.bChannelLink = Ar.Read<byte>() != 0;
    }
}

public struct AkPeakLimiterRTPCParams
{
    public float fThreshold;
    public float fRatio;
    public float fRelease;
    public float fOutputLevel;
}

public struct AkPeakLimiterNonRTPCParams
{
    public float fLookAhead;
    public bool bProcessLFE;
    public bool bChannelLink;
}
