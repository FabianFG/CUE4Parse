using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public readonly struct VMorphData(FVector positionDelta, FVector tangentZDelta, int pointIdx)
{
    public readonly FVector PositionDelta = positionDelta;
    public readonly FVector TangentZDelta = tangentZDelta;
    public readonly int PointIdx = pointIdx;

    public void Serialize(FArchiveWriter Ar)
    {
        PositionDelta.Serialize(Ar);
        TangentZDelta.Serialize(Ar);
        Ar.Write(PointIdx);
    }
}
