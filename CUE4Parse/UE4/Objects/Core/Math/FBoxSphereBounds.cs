using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.Utils;
using static System.Math;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// Structure for a combined axis aligned bounding box and bounding sphere with the same origin. (28 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FBoxSphereBounds : IUStruct
    {
        /** Holds the origin of the bounding box and sphere. */
        public FVector Origin;
        /** Holds the extent of the bounding box. */
        public FVector BoxExtent;
        /** Holds the radius of the bounding sphere. */
        public float SphereRadius;

        public FBoxSphereBounds(FVector origin, FVector boxExtent, float sphereRadius)
        {
            Origin = origin;
            BoxExtent = boxExtent;
            SphereRadius = sphereRadius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FBox GetBox() => new(Origin - BoxExtent, Origin + BoxExtent);

        public FBoxSphereBounds TransformBy(FMatrix m)
        {
            var result = new FBoxSphereBounds();

            var vecOrigin = Origin;
            var vecExtent = BoxExtent;

            var m0 = new Vector3(m.M00, m.M01, m.M02);
            var m1 = new Vector3(m.M10, m.M11, m.M12);
            var m2 = new Vector3(m.M20, m.M21, m.M22);
            var m3 = new Vector3(m.M30, m.M31, m.M32);

            var newOrigin = new Vector3(vecOrigin.X) * m0 +
                            new Vector3(vecOrigin.Y) * m1 +
                            new Vector3(vecOrigin.Z) * m2 +
                            m3;

            var newExtent = Vector3.Abs(new Vector3(vecExtent.X) * m0) +
                            Vector3.Abs(new Vector3(vecExtent.Y) * m1) +
                            Vector3.Abs(new Vector3(vecExtent.Z) * m2);

            result.BoxExtent = newExtent.ToFVector();
            result.Origin = newOrigin.ToFVector();

            var maxRadius = m0 * m0 + m1 * m1 + m2 * m2;
            maxRadius = Vector3.Max(Vector3.Max(maxRadius, new Vector3(maxRadius.Y)), new Vector3(maxRadius.Z));
            result.SphereRadius = MathF.Sqrt(maxRadius.X) * SphereRadius;

            // For non-uniform scaling, computing sphere radius from a box results in a smaller sphere.
            var boxExtentMagnitude = MathF.Sqrt(Vector3.Dot(newExtent, newExtent));
            result.SphereRadius = Min(result.SphereRadius, boxExtentMagnitude);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FBoxSphereBounds TransformBy(FTransform m)
        {
            var mat = m.ToMatrixWithScale();
            var result = TransformBy(mat);
            return result;
        }

        public override string ToString() => $"Origin=({Origin}), BoxExtent=({BoxExtent}), SphereRadius={SphereRadius}";
    }
}