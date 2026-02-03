
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

// > 118 <= 122, later removed
public readonly struct AkDiffuseReverberator
{
    public readonly uint Id;
    public readonly float Time;
    public readonly float HFRatio;
    public readonly float DryLevel;
    public readonly float WetLevel;
    public readonly float Spread;
    public readonly float Density;
    public readonly float Quality;
    public readonly float Diffusion;
    public readonly float Scale;

    public AkDiffuseReverberator(FArchive Ar)
    {
        Id = Ar.Read<uint>();
        Time = Ar.Read<float>();
        HFRatio = Ar.Read<float>();
        DryLevel = Ar.Read<float>();
        WetLevel = Ar.Read<float>();
        Spread = Ar.Read<float>();
        Density = Ar.Read<float>();
        Quality = Ar.Read<float>();
        Diffusion = Ar.Read<float>();
        Scale = Ar.Read<float>();
    }
}
