using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkSynthOneParams(FArchive Ar) : IAkPluginParam
{
    public AkSynthOneParams Params = new AkSynthOneParams(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkSynthOneParams(FArchive Ar)
{
    public AkSynthOneOperationMode eFreqMode = Ar.Read<AkSynthOneOperationMode>();
    public float fBaseFreq = Ar.Read<float>();
    public AkSynthOneFrequencyMode eOpMode = Ar.Read<AkSynthOneFrequencyMode>();
    public float fOutputLevel = Ar.Read<float>();
    public AkSynthOneNoiseType eNoiseType = Ar.Read<AkSynthOneNoiseType>();
    public float fNoiseLevel = Ar.Read<float>();
    public float fFmAmount = Ar.Read<float>();
    public bool bOverSampling = Ar.Read<byte>() != 0;
    public AkSynthOneWaveType eOsc1Waveform = Ar.Read<AkSynthOneWaveType>();
    public bool bOsc1Invert = Ar.Read<byte>() != 0;
    public int iOsc1Transpose = Ar.Read<int>();
    public float fOsc1Level = Ar.Read<float>();
    public float fOsc1Pwm = Ar.Read<float>();
    public AkSynthOneWaveType eOsc2Waveform = Ar.Read<AkSynthOneWaveType>();
    public bool bOsc2Invert = Ar.Read<byte>() != 0;
    public int iOsc2Transpose = Ar.Read<int>();
    public float fOsc2Level = Ar.Read<float>();
    public float fOsc2Pwm = Ar.Read<float>();
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkSynthOneOperationMode : byte
{
    Mix = 0x0,
    Ring = 0x1
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkSynthOneFrequencyMode : byte
{
    Specify = 0x0,
    MidiNote = 0x1
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkSynthOneNoiseType : byte
{
    White = 0x0,
    Pink = 0x1,
    Red = 0x2,
    Purple = 0x3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkSynthOneWaveType : byte
{
    Sine = 0x0,
    Triangle = 0x1,
    Square = 0x2,
    Sawtooth = 0x3
}
