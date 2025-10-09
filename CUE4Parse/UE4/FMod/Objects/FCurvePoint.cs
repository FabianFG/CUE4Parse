using System.IO;

namespace CUE4Parse.UE4.FMod.Objects;

public readonly struct FCurvePoint
{
    public readonly float X;
    public readonly float Y;
    public readonly float Shape;
    public readonly uint Type;

    public FCurvePoint(BinaryReader Ar)
    {
        X = Ar.ReadSingle();
        Y = Ar.ReadSingle();
        Shape = Ar.ReadSingle();
        Type = Ar.ReadUInt32();
    }
}
