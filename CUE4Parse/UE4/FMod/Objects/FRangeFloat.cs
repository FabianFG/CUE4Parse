using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FRangeFloat
{
    public readonly float Minimum;
    public readonly float Maximum;

    public FRangeFloat(BinaryReader Ar)
    {
        Minimum = Ar.ReadSingle();
        Maximum = Ar.ReadSingle();
    }
}
