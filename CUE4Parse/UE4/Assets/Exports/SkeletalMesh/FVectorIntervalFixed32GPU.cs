using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

public struct FVectorIntervalFixed32GPU
{
    public uint Packed;

    public int X => (int)(Packed & 0x7FF);
    public int Y => (int)((Packed >> 11) & 0x7FF);
    public int Z => (int)((Packed >> 22) & 0x3FF);

    public FVector ToVector(FVector mins, FVector ranges)
    {
        return new FVector(
            (X / 1023.0f) * ranges.X + mins.X,
            (Y / 1023.0f) * ranges.Y + mins.Y,
            (Z / 511.0f)  * ranges.Z + mins.Z
        );
    }

    public FVectorIntervalFixed32GPU(FArchive Ar)
    {
        Packed = Ar.Read<uint>();
    }
}
