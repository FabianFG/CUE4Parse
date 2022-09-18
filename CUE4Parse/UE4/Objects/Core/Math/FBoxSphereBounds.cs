using System.Numerics;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using static System.MathF;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// Structure for a combined axis aligned bounding box and bounding sphere with the same origin. (28 bytes).
    /// </summary>
    public class FBoxSphereBounds
    {
        /** Holds the origin of the bounding box and sphere. */
        public FVector Origin;
        /** Holds the extent of the bounding box. */
        public FVector BoxExtent;
        /** Holds the radius of the bounding sphere. */
        public float SphereRadius;

        public FBoxSphereBounds() { }

        public FBoxSphereBounds(FArchive Ar)
        {
            Origin = new FVector(Ar);
            BoxExtent = new FVector(Ar);
            if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
            {
                SphereRadius = (float) Ar.Read<double>();
            }
            else
            {
                SphereRadius = Ar.Read<float>();
            }
        }

        public FBoxSphereBounds(FVector origin, FVector boxExtent, float sphereRadius)
        {
            Origin = origin;
            BoxExtent = boxExtent;
            SphereRadius = sphereRadius;
        }

        public FBoxSphereBounds(FBox box, FSphere sphere)
        {
            box.GetCenterAndExtents(out Origin, out BoxExtent);
            SphereRadius = Min(BoxExtent.Size(), (sphere.Center - Origin).Size() + sphere.W);
        }

        public FBoxSphereBounds(FBox box)
        {
            box.GetCenterAndExtents(out Origin, out BoxExtent);
            SphereRadius = BoxExtent.Size();
        }

        public FBoxSphereBounds(FSphere sphere)
        {
            Origin = sphere.Center;
            BoxExtent = new FVector(sphere.W);
            SphereRadius = sphere.W;
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
            result.SphereRadius = Sqrt(maxRadius.X) * SphereRadius;

            // For non-uniform scaling, computing sphere radius from a box results in a smaller sphere.
            var boxExtentMagnitude = Sqrt(Vector3.Dot(newExtent, newExtent));
            result.SphereRadius = Min(result.SphereRadius, boxExtentMagnitude);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FBoxSphereBounds TransformBy(FTransform m) => TransformBy(m.ToMatrixWithScale());

        public override string ToString() => $"Origin=({Origin}), BoxExtent=({BoxExtent}), SphereRadius={SphereRadius}";
    }
}
