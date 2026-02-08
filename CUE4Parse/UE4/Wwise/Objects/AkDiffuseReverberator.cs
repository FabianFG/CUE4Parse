using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

// > 118 <= 122, later removed
public readonly struct AkDiffuseReverberator(FArchive Ar) : ICAkIndexable
{
    public readonly uint Id = Ar.Read<uint>();
    public readonly float Time = Ar.Read<float>();
    public readonly float HFRatio = Ar.Read<float>();
    public readonly float DryLevel = Ar.Read<float>();
    public readonly float WetLevel = Ar.Read<float>();
    public readonly float Spread = Ar.Read<float>();
    public readonly float Density = Ar.Read<float>();
    public readonly float Quality = Ar.Read<float>();
    public readonly float Diffusion = Ar.Read<float>();
    public readonly float Scale = Ar.Read<float>();
}
