using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Objects;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkGranularSynthParams(FArchive Ar) : IAkPluginParam
{
    public AkGranularSynthParams Params = new AkGranularSynthParams(Ar);
}

public class AkGranularSynthParams(FArchive Ar)
{
    public byte FilterType = Ar.Read<byte>();
    public AkChannelConfig OutputChannelConfig = new AkChannelConfig(Ar);
    public float OutputLevel = Ar.Read<float>();
    public FGranularValue GrainRate = Ar.Read<FGranularValue>();
    public FGranularValue GrainTime = Ar.Read<FGranularValue>();
    public FGranularValue Offset = Ar.Read<FGranularValue>();
    public FGranularValue Speed = Ar.Read<FGranularValue>();
    public FGranularValue Transpose = Ar.Read<FGranularValue>();
    public FGranularValue Attack = Ar.Read<FGranularValue>();
    public FGranularValue Release = Ar.Read<FGranularValue>();
    public FGranularValue Amplitude = Ar.Read<FGranularValue>();
    public FGranularValue Duration = Ar.Read<FGranularValue>();
    public FGranularValue FilterFreq = Ar.Read<FGranularValue>();
    public FGranularValue FilterQ = Ar.Read<FGranularValue>();
    public FGranularValue Azimuth = Ar.Read<FGranularValue>();
    public FGranularValue Elevation = Ar.Read<FGranularValue>();
    public FGranularValue Spread = Ar.Read<FGranularValue>();
    public FGranularValue MarkerSelect = Ar.Read<FGranularValue>();
    public FGranularValue DurationMultiplier = Ar.Read<FGranularValue>();
    public FGranularModulatorParams[] Modulators = Ar.ReadArray<FGranularModulatorParams>(4);
    public bool MidiMapTranspose = Ar.Read<byte>() != 0;
    public bool QuantizeToMarkers = Ar.Read<byte>() != 0;
    public int TransposeRoot = Ar.Read<int>();
    public int PositioningSelect = Ar.Read<int>();
    public int EnvelopeType = Ar.Read<byte>();
    public int WindowMode = Ar.Read<byte>();
    public int DurationLink = Ar.Read<byte>();
    public int MaxNumGrains = Ar.Read<int>();
    public int SelectFreqTimeGrain = Ar.Read<byte>();
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FGranularModulatorParams
{
    public int ModWaveform;
    public byte ModSelect;
    public float ModRate;
    public float ModPeriod;
    public float ModAmount;
}

[StructLayout(LayoutKind.Sequential)]
public struct FGranularValue
{
    public float Value;
    public float Mod1Depth;
    public float Mod1Quantization;
    public float Mod2Depth;
    public float Mod2Quantization;
    public float Mod3Depth;
    public float Mod3Quantization;
    public float Mod4Depth;
    public float Mod4Quantization;
}
