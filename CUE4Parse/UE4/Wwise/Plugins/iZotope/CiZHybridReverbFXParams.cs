using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZHybridReverbFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public iZHybridReverbFXParams Params = new(Ar);
}

public struct iZHybridReverbFXParams(FWwiseArchive Ar)
{
    public iZHybridReverbNonRTPCParams NonRTPC = Ar.Read<iZHybridReverbNonRTPCParams>();
    public iZHybridReverbRTPCParams RTPC = Ar.Read<iZHybridReverbRTPCParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZHybridReverbNonRTPCParams
{
    public float fDecayTime;
    public float fLowFreq;
    public float fHighFreq;
    public float fLowRatio;
    public float fMidRatio;
    public float fHighRatio;
    public uint uQuality;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZHybridReverbRTPCParams
{
    public float fEarlyGain;
    public float fTailGain;
    public float fPreDelayFront;
    public float fPreDelayRear;
    public float fFrontWet;
    public float fFrontDry;
    public float fRearWet;
    public float fRearDry;
}
