using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Plugins;

public class CAkImpacterParams(FArchive Ar) : IAkPluginParam
{
    public AkImpacterParams Params = Ar.Read<AkImpacterParams>();
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct AkImpacterParams
{
    public float Mass;
    public float Velocity;
    public float MinDuration;
    public float ImpactPosition;
    public float FMDepth;
    public int NumFiles;
    public ulong ExcitationMask;
    public ulong ModelMask;
    public float OutputLevel;
    public float MassRandom;
    public float VelocityRandom;
    public float ImpactPositionRandom;
    public float FMDepthRandom;
}
