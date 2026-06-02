using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkTremoloFXParams(FWwiseArchive Ar) : IAkPluginParam
{
    public AkTremoloFXParams Params = new(Ar);
}

[StructLayout(LayoutKind.Sequential)]
public struct AkTremoloRTPCParams
{
    public float ModDepth;
    public DSPALLParams ModParams;
    public float OutputGain;

    public AkTremoloRTPCParams(FWwiseArchive Ar)
    {
        ModDepth = Ar.Read<float>() * 0.01f;
        ModParams.LfoParams.Frequency = Ar.Read<float>();
        ModParams.LfoParams.Waveform = Ar.Read<DSPLfoWaveform>();
        ModParams.LfoParams.Smooth = Ar.Read<float>() * 0.01f;
        ModParams.LfoParams.PWM = Ar.Read<float>() * 0.01f;
        ModParams.PhaseParams = Ar.Read<DSPPhaseParams>();
        OutputGain = MathF.Pow(10f, Ar.Read<float>() * 0.05f);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct AkTremoloNonRTPCParams
{
    public bool ProcessCenter;
    public bool ProcessLFE;
}

public struct AkTremoloFXParams
{
    public AkTremoloRTPCParams RTPC;
    public AkTremoloNonRTPCParams NonRTPC;

    public AkTremoloFXParams(FWwiseArchive Ar)
    {
        RTPC = new AkTremoloRTPCParams(Ar);
        NonRTPC.ProcessCenter = Ar.Read<byte>() != 0;
        NonRTPC.ProcessLFE = Ar.Read<byte>() != 0;
    }
}
