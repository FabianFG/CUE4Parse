using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FBox2D : IUStruct
    {
        /** Holds the box's minimum point. */
        public readonly FVector2D Min;
        /** Holds the box's maximum point. */
        public readonly FVector2D Max;
        /** Holds a flag indicating whether this box is valid. */
        public readonly byte bIsValid;

        public override string ToString() => $"bIsValid={bIsValid}, Min=({Min}), Max=({Max})";
    }
}
