using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkGuitarDistortionFXParams(FArchive Ar) : IAkPluginParam
{
    public AkGuitarDistortionParams Params = new AkGuitarDistortionParams(Ar);
}

public struct AkGuitarDistortionParams
{
    public AkFilterBand[] PreEQ;
    public AkFilterBand[] PostEQ;
    public AkDistortionParams Distortion;
    public float fOutputLevel;
    public float fWetDryMix;

    public AkGuitarDistortionParams(FArchive Ar)
    {
        PreEQ = Ar.ReadArray<AkFilterBand>(3);
        PostEQ = Ar.ReadArray<AkFilterBand>(3);
        Distortion = Ar.Read<AkDistortionParams>();
        fOutputLevel = (float) Math.Pow(10f, Ar.Read<float>() * 0.05);
        fWetDryMix = Ar.Read<float>();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkFilterBand
{
    public AkFilterType eFilterType;
    public float fGain;
    public float fFrequency;
    public float fQFactor;
    public bool bOnOff;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkDistortionParams
{
    public AkDistortionType eDistortionType;
    public float fDrive;
    public float fTone;
    public float fRectification;
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
