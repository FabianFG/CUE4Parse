using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

internal class CAkFDNReverbFXParams(FArchive Ar) : IAkPluginParam
{
    public AkFDNReverbFXParams Params = new AkFDNReverbFXParams(Ar);
}

public class AkFDNReverbFXParams
{
    public AkFDNReverbRTPCParams RTPC;
    public AkFDNReverbNonRTPCParams NonRTPC;

    public AkFDNReverbFXParams(FArchive Ar)
    {
        RTPC.fReverbTime = Ar.Read<float>();
        RTPC.fHFRatio = Ar.Read<float>();
        NonRTPC.uNumberOfDelays = Ar.Read<int>();
        RTPC.fDryLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        RTPC.fWetLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        NonRTPC.fPreDelay = Ar.Read<float>();
        NonRTPC.uProcessLFE = Ar.Read<byte>() != 0;
        NonRTPC.uDelayLengthsMode = Ar.Read<AkDelayLengthsMode>();
        NonRTPC.fDelayTime = NonRTPC.uDelayLengthsMode == AkDelayLengthsMode.CUSTOM ? Ar.ReadArray<float>(NonRTPC.uNumberOfDelays) : [];
    }

    public struct AkFDNReverbRTPCParams
    {
        public float fReverbTime;
        public float fHFRatio;
        public float fDryLevel;
        public float fWetLevel;
    }

    public struct AkFDNReverbNonRTPCParams
    {
        public int uNumberOfDelays;
        public float fPreDelay;
        public bool uProcessLFE;
        public AkDelayLengthsMode uDelayLengthsMode;
        public float[] fDelayTime;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkDelayLengthsMode : uint
{
    DEFAULT = 0x0,
    CUSTOM = 0x1
}
