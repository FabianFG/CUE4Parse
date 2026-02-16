
using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkPitchShifterFXParams(FArchive Ar) : IAkPluginParam
{
    public AkPitchShifterFXParams Params = new AkPitchShifterFXParams(Ar);
}

// to-do recheck cause BN failed to decompile correctly
public struct AkPitchShifterFXParams
{
    public AkPitchVoiceParams Voice;
    public AkInputType eInputType;
    public float fDryLevel;
    public float fWetLevel;
    public float fDelayTime;
    public bool bProcessLFE;
    public bool bSyncDry;

    public AkPitchShifterFXParams(FArchive Ar)
    {
        eInputType = Ar.Read<AkInputType>();
        fDryLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fWetLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        fDelayTime = Ar.Read<float>();
        bProcessLFE = Ar.Read<byte>() != 0;
        bSyncDry = Ar.Read<byte>() != 0;
        Voice.fPitchFactor = (float) Math.Pow(2f, Ar.Read<float>() * 0.000833333354f);
        Voice.Filter = Ar.Read<AkVoiceFilterParams>();
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkInputType : uint
{
    AsInput = 0x0,
    Center = 0x1,
    Stereo = 0x2,
    ThreePointZero = 0x3,
    FourPointZero = 0x4,
    FivePointZero = 0x5
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkPitchVoiceParams
{
    public AkVoiceFilterParams Filter;
    public float fPitchFactor;
    public float fGain;
    public bool bEnable;

    public AkPitchVoiceParams(FArchive Ar)
    {
        bEnable = Ar.Read<byte>() != 0;
        fPitchFactor = (float) Math.Pow(2f, Ar.Read<float>() * 0.000833333354f);
        fGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        Filter = Ar.Read<AkVoiceFilterParams>();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AkVoiceFilterParams
{
    public AkFilterType eFilterType;
    public float fFilterGain;
    public float fFilterFrequency;
    public float fFilterQFactor;
}

