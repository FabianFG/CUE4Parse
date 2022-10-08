using System.Runtime.InteropServices;
using CUE4Parse.UE4;

namespace CUE4Parse.GameTypes.TSW.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FSpeedQuantity : IUStruct
    {
        public readonly float Value;

        public FSpeedQuantity(float InValue)
        {
            Value = InValue;
        }
    }
}
