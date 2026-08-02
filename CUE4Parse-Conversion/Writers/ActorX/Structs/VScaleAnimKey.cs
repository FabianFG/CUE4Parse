using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Writers.ActorX.Structs;

public readonly struct VScaleAnimKey(FVector scaleVector, float time)
{
    public readonly FVector ScaleVector = scaleVector;
    public readonly float Time = time;

    public void Serialize(FArchiveWriter Ar)
    {
        ScaleVector.Serialize(Ar);
        Ar.Write(Time);
    }
}
