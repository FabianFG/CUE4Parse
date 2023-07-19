using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TIntVector2<T> : IUStruct
    {
        public readonly T X;
        public readonly T Y;

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TIntVector3<T> : IUStruct
    {
        public readonly T X;
        public readonly T Y;
        public readonly T Z;

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TIntVector4<T> : IUStruct
    {
        public readonly T X;
        public readonly T Y;
        public readonly T Z;
        public readonly T W;

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}, {nameof(W)}: {W}";
        }
    }
}
