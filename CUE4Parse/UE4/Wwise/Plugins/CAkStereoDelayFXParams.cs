using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkStereoDelayFXParams(FArchive Ar) : IAkPluginParam
{
    public AkStereoDelayFXParams Params = new AkStereoDelayFXParams(Ar);
}

public struct AkStereoDelayFXParams
{
    public AkInputChannelType[] eInputType = new AkInputChannelType[2];
    public AkStereoDelayChannelParams[] StereoDelayParams = new AkStereoDelayChannelParams[2];
    public AkStereoDelayFilterParams FilterParams;
    public float fDryLevel;
    public float fWetLevel;
    public float fFrontRearBalance;
    public bool bEnableFeedback;
    public bool bEnableCrossFeed;

    public AkStereoDelayFXParams(FArchive Ar)
    {
        eInputType[0] = Ar.Read<AkInputChannelType>();
        StereoDelayParams[0] = new AkStereoDelayChannelParams(Ar);
        eInputType[1] = Ar.Read<AkInputChannelType>();
        StereoDelayParams[1] = new AkStereoDelayChannelParams(Ar);
        FilterParams = Ar.Read<AkStereoDelayFilterParams>();
        fDryLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        fWetLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        fFrontRearBalance = Ar.Read<float>();
        bEnableFeedback = Ar.Read<byte>() != 0;
        bEnableCrossFeed = Ar.Read<byte>() != 0;
    }

    public struct AkStereoDelayChannelParams(FArchive Ar)
    {
        public float fDelayTime = Ar.Read<float>();
        public float fFeedback = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        public float fCrossFeed = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AkStereoDelayFilterParams
    {
        public AkFilterType eFilterType;
        public float fFilterGain;
        public float fFilterFrequency;
        public float fFilterQFactor;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AkInputChannelType : uint
    {
        LEFT_OR_RIGHT = 0x0,
        CENTER = 0x1,
        DOWNMIX = 0x2,
        NONE = 0x3
    }
}
