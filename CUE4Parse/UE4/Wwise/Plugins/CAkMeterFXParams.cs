using System.Drawing;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkMeterFXParams(FArchive Ar) : IAkPluginParam
{
    public AkMeterFXParams Params = new AkMeterFXParams(Ar);
}

public struct AkMeterFXParams(FArchive Ar)
{
    public AkMeterRTPCParams RTPC = new AkMeterRTPCParams(Ar);
    public AkMeterNonRTPCParams NonRTPC = new AkMeterNonRTPCParams(Ar);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AkMeterRTPCParams(FArchive Ar)
    {
        public float fAttack = Ar.Read<float>();
        public float fRelease = Ar.Read<float>();
        public float fMin = Ar.Read<float>();
        public float fMax = Ar.Read<float>();
        public float fHold = Ar.Read<float>();
        public bool bInfiniteHold = WwiseVersions.Version >= 145 && Ar.Read<byte>() != 0;
    }

    public struct AkMeterNonRTPCParams
    {
        public AkMeterMode? eMode;
        public AkMeterScope? eScope;
        public bool bApplyDownstreamVolume;
        public uint uGameParamID;

        public AkMeterNonRTPCParams(FArchive Ar)
        {
            eMode = WwiseVersions.Version <= 88 ? null : Ar.Read<AkMeterMode>();
            eScope = WwiseVersions.Version >= 125 ? Ar.Read<AkMeterScope>() : null;
            bApplyDownstreamVolume = Ar.Read<byte>() != 0;
            uGameParamID = Ar.Read<uint>();
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AkMeterMode : byte
    {
        AkMeterMode_Peak = 0x0,
        AkMeterMode_RMS = 0x1
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AkMeterScope : byte
    {
        Global = 0,
        GameObject = 1
    }
}
