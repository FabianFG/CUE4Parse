using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class VMorphData
{
    public readonly FVector PositionDelta;
    public readonly FVector TangentZDelta;
    public readonly int PointIdx;

    public VMorphData(FVector positionDelta, FVector tangentZDelta, int pointIdx)
    {
        PositionDelta = positionDelta;
        TangentZDelta = tangentZDelta;
        PointIdx = pointIdx;
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        PositionDelta.Serialize(Ar);
        TangentZDelta.Serialize(Ar);
        Ar.Write(PointIdx);
    }
}