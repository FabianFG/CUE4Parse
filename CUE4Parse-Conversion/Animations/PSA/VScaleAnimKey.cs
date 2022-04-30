using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Animations.PSA
{
    public class VScaleAnimKey
    {
        public FVector ScaleVector;
        public float Time;

        public void Serialize(FArchiveWriter Ar)
        {
            ScaleVector.Serialize(Ar);
            Ar.Write(Time);
        }
    }
}
