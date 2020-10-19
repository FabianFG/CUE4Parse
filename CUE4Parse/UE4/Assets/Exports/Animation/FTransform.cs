using CUE4Parse.UE4.Objects.Core.Math;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FTransform : IUStruct
    {
        public readonly FQuat Rotation;
        public readonly FVector Translation;
        public readonly FVector Scale3D;
    }
}
