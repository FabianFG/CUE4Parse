using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Animations.PSA;

public class VScaleAnimKey : ISerializable
{
    public FVector ScaleVector;
    public float Time;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(ScaleVector);
        Ar.Write(Time);
    }
}