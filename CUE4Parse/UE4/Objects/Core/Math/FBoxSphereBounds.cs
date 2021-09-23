using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// Structure for a combined axis aligned bounding box and bounding sphere with the same origin. (28 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FBoxSphereBounds : IUStruct
    {
        /** Holds the origin of the bounding box and sphere. */
        public readonly FVector Origin;
        /** Holds the extent of the bounding box. */
        public readonly FVector BoxExtent;
        /** Holds the radius of the bounding sphere. */
        public readonly float SphereRadius;

        public FBoxSphereBounds(FVector origin, FVector boxExtent, float sphereRadius)
        {
            Origin = origin;
            BoxExtent = boxExtent;
            SphereRadius = sphereRadius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FBox GetBox() => new(Origin - BoxExtent, Origin + BoxExtent);

        public override string ToString() => $"Origin=({Origin}), BoxExtent=({BoxExtent}), SphereRadius={SphereRadius}";
    }
}
