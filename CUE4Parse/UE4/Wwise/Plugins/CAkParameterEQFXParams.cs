using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public interface IAkParametricEQFXParams;

public class CAkParameterEQFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public IAkParametricEQFXParams Params = Ar.Version >= 172 ? new AkParametricEQFXParams(Ar) : new AkParametricEQFXParamsOld(Ar);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EQModuleParamsOld
{
    public AkFilterTypeOld FilterType;
    public float Gain;
    public float Frequency;
    public float QFactor;
    public bool OnOff;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EQModuleParamsDynamic
{
    public float BandDynamicsThresholdDb;
    public float BandDynamicsRangeDb;
    public float BandDynamicsAttackMs;
    public float BandDynamicsReleaseMs;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EQModuleParamsStatic
{
    public AkFilterType FilterType;
    public byte BandRolloff;
    public float Frequency;
    public float GainDb;
    public float QFactor;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EQModuleParams
{
    public EQModuleParamsStatic Static;
    public EQModuleParamsDynamic Dynamic;
}


public struct AkParametricEQFXParamsOld : IAkParametricEQFXParams
{
    public EQModuleParamsOld[] Bands;
    public float OutputLevel;
    public bool ProcessLFE;

    public AkParametricEQFXParamsOld(FWwiseArchive Ar)
    {
        Bands = Ar.ReadArray<EQModuleParamsOld>(3);
        OutputLevel = Ar.Read<float>();
        ProcessLFE = Ar.Read<byte>() != 0;
    }
}
public struct AkParametricEQFXParams : IAkParametricEQFXParams
{
    public EQModuleParams[] Bands;
    public float OutputLevel;
    public bool ProcessLFE;
    public uint SidechainId;
    public bool SidechainGlobalScope;

    public AkParametricEQFXParams(FWwiseArchive Ar)
    {
        OutputLevel = MathF.Exp(Ar.Read<float>() * 0.05f);
        ProcessLFE = Ar.Read<byte>() != 0;
        SidechainId = Ar.Read<uint>();
        SidechainGlobalScope = Ar.Read<byte>() != 0;
        var numBands = Ar.Read<byte>();
        var bandEnabledBitfield = Ar.Read<uint>();
        var bandDynamicsEnabledBitfield = Ar.Read<uint>();
        var staticPart = Ar.ReadArray<EQModuleParamsStatic>(numBands);
        var dynamicPart = bandDynamicsEnabledBitfield != 0 ? Ar.ReadArray<EQModuleParamsDynamic>(numBands) : new EQModuleParamsDynamic[numBands];
        Bands = new EQModuleParams[numBands];
        for (int i = 0; i < numBands; i++)
        {
            Bands[i].Static = staticPart[i];
            Bands[i].Dynamic = dynamicPart[i];
        }
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkFilterTypeOld : uint
{
    LowShelf = 0x0,
    PeakingEQ = 0x1,
    HighShelf = 0x2,
    LowPass = 0x3,
    HighPass = 0x4,
    BandPass = 0x5,
    Notch = 0x6
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AkFilterType : byte
{
    LowPass = 0x0,
    HighPass = 0x1,
    BandPass = 0x2,
    Notch = 0x3,
    LowShelf = 0x4,
    HighShelf = 0x5,
    PeakingEQ = 0x6,
    LowPassQ = 0x7,
    HighPassQ = 0x8
}
