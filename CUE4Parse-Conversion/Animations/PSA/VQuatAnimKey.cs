using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Animations.PSA;

public class VQuatAnimKey : ISerializable
{
    /** Relative to parent */
    public FVector Position;
    /** Relative to parent */
    public FQuat Orientation;
    /** The duration until the next key (end key wraps to first ...) */
    public float Time;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(Position);
        Ar.Serialize(Orientation);
        Ar.Write(Time);
    }
}