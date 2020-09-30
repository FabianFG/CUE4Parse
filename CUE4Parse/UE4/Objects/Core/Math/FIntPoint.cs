using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIntPoint : IUStruct
    {
        public readonly uint X;
        public readonly uint Y;
    }
}