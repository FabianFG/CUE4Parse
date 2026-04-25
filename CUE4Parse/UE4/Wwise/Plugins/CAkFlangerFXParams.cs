using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkFlangerFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkFlangerFXParams Params = new(Ar);
}

[StructLayout(LayoutKind.Sequential)]
public struct DSPLfoParams
{
    public float Frequency;
    public DSPLfoWaveform Waveform;
    public float Smooth;
    public float PWM;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DSPPhaseParams
{
    public float PhaseOffset;
    public DSPPhaseMode PhaseMode;
    public float PhaseSpread;
}

[StructLayout(LayoutKind.Sequential)]
public struct DSPALLParams
{
    public DSPLfoParams LfoParams;
    public DSPPhaseParams PhaseParams;
}

[StructLayout(LayoutKind.Sequential)]
public struct AkFlangerRTPCParams
{
    public float DryLevel;
    public float FfwdLevel;
    public float FbackLevel;
    public float ModDepth;
    public DSPALLParams ModParams;
    public float OutputLevel;
    public float WetDryMix;
    public bool HasChanged;

    public AkFlangerRTPCParams(FWwiseArchive Ar)
    {
        DryLevel = Ar.Read<float>();
        FfwdLevel = Ar.Read<float>();
        FbackLevel = Ar.Read<float>();
        ModDepth = Ar.Read<float>() * 0.01f;
        ModParams.LfoParams.Frequency = Ar.Read<float>();
        ModParams.LfoParams.Waveform = Ar.Read<DSPLfoWaveform>();
        ModParams.LfoParams.Smooth = Ar.Read<float>() * 0.01f;
        ModParams.LfoParams.PWM = Ar.Read<float>() * 0.01f;
        ModParams.PhaseParams = Ar.Read<DSPPhaseParams>();
        OutputLevel = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
        WetDryMix = Ar.Read<float>();
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct AkFlangerNonRTPCParams
{
    public float DelayTime;
    public bool EnableLFO;
    public bool ProcessCenter;
    public bool ProcessLFE;
    public bool HasChanged;
}

[StructLayout(LayoutKind.Sequential)]
public struct AkFlangerFXParams
{
    public AkFlangerRTPCParams RTPC;
    public AkFlangerNonRTPCParams NonRTPC;

    public AkFlangerFXParams(FWwiseArchive Ar)
    {
        NonRTPC.DelayTime = Ar.Read<float>();
        RTPC = new AkFlangerRTPCParams(Ar);
        NonRTPC.EnableLFO = Ar.Read<byte>() != 0;
        NonRTPC.ProcessCenter = Ar.Read<byte>() != 0;
        NonRTPC.ProcessLFE = Ar.Read<byte>() != 0;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DSPLfoWaveform : uint
{
    First = 0x0,
#pragma warning disable CA1069
    Sine = 0x0,
#pragma warning restore CA1069
    Triangle = 0x1,
    Square = 0x2,
    SawUp = 0x3,
    SawDown = 0x4,
    Num = 0x5
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DSPPhaseMode : uint
{
    LeftRight = 0x0,
    FrontRear = 0x1,
    Circular = 0x2,
    Random = 0x3
}
