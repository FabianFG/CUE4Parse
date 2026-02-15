using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins.iZotope;

public class CiZTrashFiltersFXParams(FArchive Ar) : IAkPluginParam
{
    public iZTrashFiltersFXParams Params = Ar.Read<iZTrashFiltersFXParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct iZTrashFiltersFXParams
{
    public uint Filter1Type;
    public float Filter1Frequency;
    public float Filter1Q;
    public float Filter1Resonance;
    public float Filter1Gain;
    public uint Filter1Trigger;
    public uint Filter1LFOType;
    public float Filter1LFOPeriod;
    public float Filter1LFOTargetFreq;
    public float Filter1LFOTargetGain;
    public float Filter1LFOTargetQ;
    public float Filter1LFOTargetRes;

    public uint Filter2Type;
    public float Filter2Frequency;
    public float Filter2Q;
    public float Filter2Resonance;
    public float Filter2Gain;
    public uint Filter2Trigger;
    public uint Filter2LFOType;
    public float Filter2LFOPeriod;
    public float Filter2LFOTargetFreq;
    public float Filter2LFOTargetGain;
    public float Filter2LFOTargetQ;
    public float Filter2LFOTargetRes;

    public uint Filter3Type;
    public float Filter3Frequency;
    public float Filter3Q;
    public float Filter3Resonance;
    public float Filter3Gain;
    public uint Filter3Trigger;
    public uint Filter3LFOType;
    public float Filter3LFOPeriod;
    public float Filter3LFOTargetFreq;
    public float Filter3LFOTargetGain;
    public float Filter3LFOTargetQ;
    public float Filter3LFOTargetRes;
}
