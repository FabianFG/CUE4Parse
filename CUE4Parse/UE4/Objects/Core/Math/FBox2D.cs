using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public readonly struct FBox2D : IUStruct
    {
        /** Holds the box's minimum point. */
        public readonly FVector2D Min;
        /** Holds the box's maximum point. */
        public readonly FVector2D Max;
        /** Holds a flag indicating whether this box is valid. */
        public readonly bool bIsValid;

        public FBox2D(FAssetArchive Ar)
        {
            Min = Ar.Read<FVector2D>();
            Max = Ar.Read<FVector2D>();
            bIsValid = Ar.ReadFlag();
        }

        public override string ToString() => $"bIsValid={bIsValid}, Min=({Min}), Max=({Max})";
    }
}
