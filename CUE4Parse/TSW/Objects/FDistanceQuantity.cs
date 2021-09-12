using System.Runtime.InteropServices;
using CUE4Parse.UE4;

namespace CUE4Parse.TSW.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FDistanceQuantity : IUStruct
    {
        public readonly float Value;

        public FDistanceQuantity(float InValue)
        {
            Value = InValue;
        }
    }
}
