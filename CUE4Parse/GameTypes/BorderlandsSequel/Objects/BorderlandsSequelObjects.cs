using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.BorderlandsSequel.Objects;

public struct FVectorShort48
{
    public ushort X;
    public ushort Y;
    public ushort Z;

    public FVector ToVector(FVector mins, FVector ranges)
    {
        return new FVector(
            (X / 65535.0f) * ranges.X + mins.X,
            (Y / 65535.0f) * ranges.Y + mins.Y,
            (Z / 65535.0f) * ranges.Z + mins.Z
        );
    }
}

public struct FVectorShort64
{
    public short X;
    public short Y;
    public short Z;
    public short W;

    public FVector ToVector(FVector mins, FVector ranges)
    {
        return new FVector(
            (X / 32767.0f) * ranges.X + mins.X,
            (Y / 32767.0f) * ranges.Y + mins.Y,
            (Z / 32767.0f) * ranges.Z + mins.Z
        );
    }
}
