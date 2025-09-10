using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math;

public class TBox3<T> : IUStruct
{
    /** Holds the box's minimum point. */
    public readonly TIntVector3<T> Min;
    /** Holds the box's maximum point. */
    public readonly TIntVector3<T> Max;
    /** Holds a flag indicating whether this box is valid. */
    public readonly byte bIsValid;

    public TBox3() { }

    public TBox3(FArchive Ar)
    {
        Min = Ar.Read<TIntVector3<T>>();
        Max = Ar.Read<TIntVector3<T>>();
        bIsValid = Ar.Read<byte>();
    }

    public override string ToString() => $"bIsValid={bIsValid}, Min=({Min}), Max=({Max})";
}
