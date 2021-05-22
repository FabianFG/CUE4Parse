using CUE4Parse.UE4.Objects.Core.Math;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.Animation
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FTransform : IUStruct
    {

        public static FTransform Identity = new() {Rotation = FQuat.Identity, Translation = FVector.ZeroVector, Scale3D = new FVector(1, 1, 1)};
        
        public FQuat Rotation;
        public FVector Translation;
        public FVector Scale3D;

        public bool ContainsNan()
        {
            return Translation.ContainsNaN() || Rotation.ContainsNaN() || Scale3D.ContainsNaN();
        }
        
    }
}
