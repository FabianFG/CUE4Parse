using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public readonly struct VQuatAnimKey(FVector position, FQuat orientation, float time)
{
    /** Relative to parent */
    public readonly FVector Position = position;
    /** Relative to parent */
    public readonly FQuat Orientation = orientation;
    /** The duration until the next key (end key wraps to first ...) */
    public readonly float Time = time;

    public void Serialize(FArchiveWriter Ar)
    {
        Position.Serialize(Ar);
        Orientation.Serialize(Ar);
        Ar.Write(Time);
    }
}
