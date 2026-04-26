using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkMeterFXParams : IAkPluginParam
{
    public AkMeterFXParams? Params;
    public AkMeterBallisticParams? BallisticParams;
    public AkMeterParams? MeterParams;

    public CAkMeterFXParams(FWwiseArchive Ar)
    {
        if (Ar.Version > 154)
        {
            MeterParams = new AkMeterParams(Ar);
            BallisticParams = new AkMeterBallisticParams(Ar);
        }
        else
        {
            Params = new AkMeterFXParams(Ar);
        }
    }
}

public struct AkMeterFXParams(FWwiseArchive Ar)
{
    public AkMeterRTPCParams RTPC = new(Ar);
    public AkMeterNonRTPCParams NonRTPC = new(Ar);

    public struct AkMeterRTPCParams(FWwiseArchive Ar)
    {
        public float fAttack = Ar.Read<float>();
        public float fRelease = Ar.Read<float>();
        public float fMin = Ar.Read<float>();
        public float fMax = Ar.Read<float>();
        public float fHold = Ar.Read<float>();
        public bool bInfiniteHold = Ar.Version >= 144 && Ar.Read<byte>() != 0;
    }

    public struct AkMeterNonRTPCParams
    {
        public AkMeterMode? eMode;
        public AkMeterScope? eScope;
        public bool bApplyDownstreamVolume;
        public uint uGameParamID;

        public AkMeterNonRTPCParams(FWwiseArchive Ar)
        {
            eMode = Ar.Version <= 88 ? null : Ar.Read<AkMeterMode>();
            eScope = Ar.Version >= 125 ? Ar.Read<AkMeterScope>() : null;
            bApplyDownstreamVolume = Ar.Read<byte>() != 0;
            uGameParamID = Ar.Read<uint>();
        }
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkMeterMode : byte
{
    Peak = 0x0,
    RMS = 0x1
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkMeterScope : byte
{
    Global = 0,
    GameObject = 1
}

public struct AkMeterParams(FWwiseArchive Ar)
{
    public AkMeterMode eMode = Ar.Read<AkMeterMode>();
    public AkMeterScope eScope = Ar.Read<AkMeterScope>();
    public EAkChannelConfig mixdownCfg = Ar.Read<EAkChannelConfig>();
    public bool bApplyDownstreamVolume = Ar.Read<byte>() != 0;
    public bool bInfiniteHold = Ar.Read<byte>() != 0;
}

public struct AkMeterBallisticParams(FWwiseArchive Ar)
{
    public uint uGameParamID = Ar.Read<uint>();
    public float fAttack = Ar.Read<float>();
    public float fRelease = Ar.Read<float>();
    public float fMin = Ar.Read<float>();
    public float fMax = Ar.Read<float>();
    public float fHold = Ar.Read<float>();
}

public struct AkMultibandMeterBandParams(FWwiseArchive Ar)
{
    public bool bFilterEnabled = Ar.Read<byte>() != 0;
    public byte uNumCascadesLow = Ar.Read<byte>();
    public byte uNumCascadesHigh = Ar.Read<byte>();
    public float fFrequencyLow = Ar.Read<float>();
    public float fFrequencyHigh = Ar.Read<float>();
}

public class CAkMultibandMeterFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkMeterParams MeterParams = new(Ar);
    public AkMeterBallisticParams[] BallisticParams= Ar.ReadArray(5, () => new AkMeterBallisticParams(Ar));
    public AkMultibandMeterBandParams[] BandParamss = Ar.ReadArray(5, () => new AkMultibandMeterBandParams(Ar));
}
