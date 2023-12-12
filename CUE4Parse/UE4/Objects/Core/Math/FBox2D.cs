using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Core.Math;

public class FBox2D : IUStruct, ISerializable
{
    /** Holds the box's minimum point. */
    public readonly FVector2D Min;
    /** Holds the box's maximum point. */
    public readonly FVector2D Max;
    /** Holds a flag indicating whether this box is valid. */
    public readonly byte bIsValid;

    public FBox2D() { }

    public FBox2D(FArchive Ar)
    {
        Min = new FVector2D(Ar);
        Max = new FVector2D(Ar);
        bIsValid = Ar.Read<byte>();
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(Min);
        Ar.Serialize(Max);
        Ar.Write(bIsValid);
    }
    
    public override string ToString() => $"bIsValid={bIsValid}, Min=({Min}), Max=({Max})";
}