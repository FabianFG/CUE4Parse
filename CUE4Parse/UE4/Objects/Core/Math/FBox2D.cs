using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public class FBox2D : IUStruct
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

        public override string ToString() => $"bIsValid={bIsValid}, Min=({Min}), Max=({Max})";
    }
}
