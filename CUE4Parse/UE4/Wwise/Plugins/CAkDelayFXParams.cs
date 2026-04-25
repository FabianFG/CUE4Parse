using System;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkDelayFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkDelayFXParams Params = new(Ar);
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

    public AkDelayFXParams(FWwiseArchive Ar)
    {
        NonRTPC.fDelayTime = Ar.Read<float>();
        RTPC.fFeedback = Ar.Read<float>() * 0.01f;
        RTPC.fWetDryMix = Ar.Read<float>() * 0.01f;
        RTPC.fOutputLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        RTPC.bFeedbackEnabled = Ar.Read<byte>() != 0;
        NonRTPC.bProcessLFE = Ar.Read<byte>() != 0;
    }
}
