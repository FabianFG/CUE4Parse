using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FMappingPoint
{
    public readonly float X;
    public readonly float Y;

    public FMappingPoint(BinaryReader Ar)
    {
        X = Ar.ReadSingle();
        Y = Ar.ReadSingle();
    }
}
