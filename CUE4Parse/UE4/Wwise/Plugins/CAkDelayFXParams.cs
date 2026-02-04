using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkDelayFXParams(FArchive Ar) : IAkPluginParam
{
    public AkDelayFXParams Params = new AkDelayFXParams(Ar);
}

public struct AkDelayRTPCParams
{
    public float fFeedback;
    public float fWetDryMix;
    public float fOutputLevel;
    public bool bFeedbackEnabled;
}

public struct AkDelayNonRTPCParams
{
    public float fDelayTime;
    public bool bProcessLFE;
}

public struct AkDelayFXParams
{
    public AkDelayRTPCParams RTPC;
    public AkDelayNonRTPCParams NonRTPC;

    public AkDelayFXParams(FArchive Ar)
    {
        NonRTPC.fDelayTime = Ar.Read<float>();
        RTPC.fFeedback = Ar.Read<float>() * 0.01f;
        RTPC.fWetDryMix = Ar.Read<float>() * 0.01f;
        RTPC.fOutputLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        RTPC.bFeedbackEnabled = Ar.Read<byte>() != 0;
        NonRTPC.bProcessLFE = Ar.Read<byte>() != 0;
    }
}
