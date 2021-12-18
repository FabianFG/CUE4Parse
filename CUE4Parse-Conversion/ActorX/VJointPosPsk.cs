using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.ActorX
{
    public struct VJointPosPsk
    {
        public FQuat Orientation;
        public FVector Position;
        public float Length;
        public FVector Size;

        public void Serialize(FArchiveWriter Ar)
        {
            Orientation.Serialize(Ar);
            Position.Serialize(Ar);
            Ar.Write(Length);
            Size.Serialize(Ar);
        }
    }
}
