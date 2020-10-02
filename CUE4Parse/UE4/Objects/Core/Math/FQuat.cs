using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FQuat : IUStruct
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float W;
    }
}
