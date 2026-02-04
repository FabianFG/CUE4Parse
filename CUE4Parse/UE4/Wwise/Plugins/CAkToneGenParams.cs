using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkToneGenParams(FArchive Ar) : IAkPluginParam
{
    public AkToneGenParams Params = Ar.Read<AkToneGenParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkToneGenParams
{
    public float fGain;
    public float fStartFreq;
    public float fStopFreq;
    public AkToneGenStaticParams staticParams;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkToneGenStaticParams
{
    public float fStartFreqRandMin;
    public float fStartFreqRandMax;
    public byte bFreqSweep;
    public AkToneGenSweep eGenSweep;
    public float fStopFreqRandMin;
    public float fStopFreqRandMax;
    public AkToneGenType eGenType;
    public AkToneGenMode eGenMode;
    public float fFixDur;
    public float fAttackDur;
    public float fDecayDur;
    public float fSustainDur;
    public float fSustainVal;
    public float fReleaseDur;
    //maybe enum fmt_ch -> enum Audio::EAudioMixerChannel::Type
    public uint uChannelMask;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkToneGenSweep : uint
{
    LIN = 0x0,
    LOG = 0x1
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkToneGenType : uint
{
    SINE = 0x0,
    TRIANGLE = 0x1,
    SQUARE = 0x2,
    SAWTOOTH = 0x3,
    WHITENOISE = 0x4,
    PINKNOISE = 0x5
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkToneGenMode : uint
{
    FIX = 0x0,
    ENV = 0x1
}
