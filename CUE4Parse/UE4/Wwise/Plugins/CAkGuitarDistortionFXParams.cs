using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkGuitarDistortionFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkGuitarDistortionParams Params = new(Ar);
}

public struct AkGuitarDistortionParams
{
    public AkFilterBand[] PreEQ;
    public AkFilterBand[] PostEQ;
    public AkDistortionParams Distortion;
    public float OutputLevel;
    public float WetDryMix;

    public AkGuitarDistortionParams(FWwiseArchive Ar)
    {
        PreEQ = Ar.ReadArray<AkFilterBand>(3);
        PostEQ = Ar.ReadArray<AkFilterBand>(3);
        Distortion = Ar.Read<AkDistortionParams>();
        OutputLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        WetDryMix = Ar.Read<float>();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkFilterBand
{
    public AkFilterTypeOld FilterType;
    public float Gain;
    public float Frequency;
    public float QFactor;
    public bool OnOff;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkDistortionParams
{
    public AkDistortionType DistortionType;
    public float Drive;
    public float Tone;
    public float Rectification;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkDistortionType : uint
{
    None = 0x0,
    Overdrive = 0x1,
    Heavy = 0x2,
    Fuzz = 0x3,
    Clip = 0x4
}
