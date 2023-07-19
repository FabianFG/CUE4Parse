using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FBox : IUStruct
    {
        /// <summary>
        /// Holds the box's minimum point.
        /// </summary>
        public FVector Min;

        /// <summary>
        /// Holds the box's maximum point.
        /// </summary>
        public FVector Max;

        /// <summary>
        /// Holds a flag indicating whether this box is valid.
        /// </summary>
        public byte IsValid; // It's a bool

        /// <summary>
        /// Creates and initializes a new box from the specified extents.
        /// </summary>
        /// <param name="min">The box's minimum point.</param>
        /// <param name="max">The box's maximum point.</param>
        public FBox(FVector min, FVector max, byte isValid = 1)
        {
            Min = min;
            Max = max;
            IsValid = isValid;
        }

        public FBox(FArchive Ar)
        {
            Min = new FVector(Ar);
            Max = new FVector(Ar);
            IsValid = Ar.Read<byte>();
        }

        public FBox(FVector[] points)
        {
            Min = new FVector(0f, 0f, 0f);
            Max = new FVector(0f, 0f, 0f);
            IsValid = 0;
            foreach (var it in points)
            {
                Min += it;
                Max += it;
            }
        }

        public FBox(FBox box)
        {
            Min = box.Min;
            Max = box.Max;
            IsValid = box.IsValid;
        }

        public bool Equals(FBox other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        public override bool Equals(object? obj)
        {
            return obj is FBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Min.GetHashCode() * 397) ^ Max.GetHashCode();
            }
        }

        public static FBox operator +(FBox a, FVector other)
        {
            if (a.IsValid != 0)
            {
                return new FBox(
                    new FVector(System.Math.Min(a.Min.X, other.X), System.Math.Min(a.Min.Y, other.Y), System.Math.Min(a.Min.Z, other.Z)),
                    new FVector(System.Math.Max(a.Max.X, other.X), System.Math.Max(a.Max.Y, other.Y), System.Math.Max(a.Max.Z, other.Z)));
            }
            return new FBox(other, other, 1);
        }

        public static FBox operator +(FBox a, FBox other)
        {
            if (a.IsValid != 0)
            {
                return new FBox(
                    new FVector(System.Math.Min(a.Min.X, other.Min.X), System.Math.Min(a.Min.Y, other.Min.Y), System.Math.Min(a.Min.Z, other.Min.Z)),
                    new FVector(System.Math.Max(a.Max.X, other.Max.X), System.Math.Max(a.Max.Y, other.Max.Y), System.Math.Max(a.Max.Z, other.Max.Z)));
            }
            return new FBox(other.Min, other.Max, other.IsValid);
        }

        public static FBox operator *(FBox a, float scale) =>
            new FBox(
                new FVector(a.Min.X * scale, a.Min.Y * scale, a.Min.Z * scale),
                new FVector(a.Max.X * scale, a.Max.Y * scale, a.Max.Z * scale));

        public FVector this[int i]
        {
            get
            {
                return i switch
                {
                    0 => Min,
                    1 => Max,
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }

        /// <summary>
        /// Calculates the distance of a point to this box.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>The distance.</returns>
        public float ComputeSquaredDistanceToPoint(FVector point) =>
            FVector.ComputeSquaredDistanceFromBoxToPoint(Min, Max, point);

        /// <summary>
        /// Increases the box size.
        /// </summary>
        /// <param name="w">The size to increase the volume by.</param>
        /// <returns>A new bounding box.</returns>
        public FBox ExpandBy(float w) => new FBox(Min - new FVector(w, w, w), Max + new FVector(w, w, w));

        /// <summary>
        /// Increases the box size.
        /// </summary>
        /// <param name="v">The size to increase the volume by.</param>
        /// <returns>A new bounding box.</returns>
        public FBox ExpandBy(FVector v) => new FBox(Min - v, Max + v);

        /// <summary>
        /// Increases the box size.
        /// </summary>
        /// <param name="neg">The size to increase the volume by in the negative direction (positive values move the bounds outwards)</param>
        /// <param name="pos">The size to increase the volume by in the positive direction (positive values move the bounds outwards)</param>
        /// <returns>A new bounding box.</returns>
        public FBox ExpandBy(FVector neg, FVector pos) => new FBox(Min - neg, Max + pos);

        /// <summary>
        /// Shifts the bounding box position.
        /// </summary>
        /// <param name="offset">The vector to shift the box by.</param>
        /// <returns>A new bounding box.</returns>
        public FBox ShiftBy(FVector offset) => new FBox(Min + offset, Max + offset);

        /// <summary>
        /// Moves the center of bounding box to new destination.
        /// </summary>
        /// <param name="destination">The destination point to move center of box to.</param>
        /// <returns>A new bounding box.</returns>
        public FBox MoveTo(FVector destination)
        {
            var offset = destination - GetCenter();
            return new FBox(Min + offset, Max + offset);
        }

        /// <summary>
        /// Gets the center point of this box.
        /// </summary>
        /// <returns>The center point.</returns>
        public FVector GetCenter() => (Min + Max) * 0.5f;

        /// <summary>
        /// Gets the center and extents of this box.
        /// </summary>
        /// <param name="center">(out) Will contain the box center point.</param>
        /// <param name="extents">(out) Will contain the extent around the center.</param>
        public void GetCenterAndExtents(out FVector center, out FVector extents)
        {
            extents = GetExtent();
            center = Min + extents;
        }

        /// <summary>
        /// Calculates the closest point on or inside the box to a given point in space.
        /// </summary>
        /// <param name="point">The point in space.</param>
        /// <returns>The closest point on or inside the box.</returns>
        public FVector GetClosestPointTo(FVector point)
        {
            // start by considering the point inside the box
            var closestPoint = point;

            // now clamp to inside box if it's outside
            if (point.X < Min.X)
            {
                closestPoint.X = Min.X;
            } else if (point.X > Max.X)
            {
                closestPoint.X = Max.X;
            }

            // now clamp to inside box if it's outside
            if (point.Y < Min.Y)
            {
                closestPoint.Y = Min.Y;
            } else if (point.Y > Max.Y)
            {
                closestPoint.Y = Max.Y;
            }

            // Now clamp to inside box if it's outside.
            if (point.Z < Min.Z)
            {
                closestPoint.Z = Min.Z;
            } else if (point.Z > Max.Z)
            {
                closestPoint.Z = Max.Z;
            }

            return closestPoint;
        }

        public FVector GetExtent() => (Max - Min) * 0.5f;
        public FVector GetSize() => Max - Min;
        public float GetVolume() => (Max.X - Min.X) * (Max.Y - Min.Y) * (Max.Z - Min.Z);

        /// <summary>
        /// Checks whether the given bounding box intersects this bounding box.
        /// </summary>
        /// <param name="other">The bounding box to intersect with.</param>
        /// <returns></returns>
        public bool Intersects(FBox other)
        {
            if ((Min.X > other.Max.X) || (other.Min.X > Max.X)) {
                return false;
            }

            if ((Min.Y > other.Max.Y) || (other.Min.Y > Max.Y)) {
                return false;
            }

            if ((Min.Z > other.Max.Z) || (other.Min.Z > Max.Z)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the given bounding box intersects this bounding box in the XY plane.
        /// </summary>
        /// <param name="other">The bounding box to intersect with.</param>
        /// <returns>true if the boxes intersect in the XY Plane, false otherwise.</returns>
        public bool IntersectsXY(FBox other)
        {
            if ((Min.X > other.Max.X) || (other.Min.X > Max.X)) {
                return false;
            }

            if ((Min.Y > other.Max.Y) || (other.Min.Y > Max.Y)) {
                return false;
            }

            return true;
        }

        public FBox Overlap(FBox other)
        {
            if (!Intersects(other))
            {
                return new FBox(new FVector(0f, 0f, 0f), new FVector(0f, 0f, 0f));
            }

            // otherwise they overlap
            // so find overlapping box
            var minVector = new FVector();
            var maxVector = new FVector();

            minVector.X = System.Math.Max(Min.X, other.Min.X);
            maxVector.X = System.Math.Min(Max.X, other.Max.X);

            minVector.Y = System.Math.Max(Min.Y, other.Min.Y);
            maxVector.Y = System.Math.Min(Max.Y, other.Max.Y);

            minVector.Z = System.Math.Max(Min.Z, other.Min.Z);
            maxVector.Z = System.Math.Min(Max.Z, other.Max.Z);

            return new FBox(minVector, maxVector);
        }

        /// <summary>
        /// Checks whether the given location is inside this box.
        /// </summary>
        /// <param name="in">The location to test for inside the bounding volume.</param>
        /// <returns>true if location is inside this volume.</returns>
        public bool IsInside(FVector @in) => (@in.X > Min.X) && (@in.X < Max.X) && (@in.Y > Min.Y) && (@in.Y < Max.Y) &&
                                             (@in.Z > Min.Z) && (@in.Z < Max.Z);

        /// <summary>
        /// Checks whether the given location is inside or on this box.
        /// </summary>
        /// <param name="in">The location to test for inside the bounding volume.</param>
        /// <returns>true if location is inside or on this volume.</returns>
        public bool IsInsideOrOn(FVector @in) => (@in.X >= Min.X) && (@in.X <= Max.X) && (@in.Y >= Min.Y) && (@in.Y <= Max.Y) &&
                                             (@in.Z >= Min.Z) && (@in.Z <= Max.Z);

        /// <summary>
        /// Checks whether a given box is fully encapsulated by this box.
        /// </summary>
        /// <param name="other">The box to test for encapsulation within the bounding volume.</param>
        /// <returns>true if box is inside this volume.</returns>
        public bool IsInside(FBox other) => IsInside(other.Min) && IsInside(other.Max);

        /// <summary>
        /// Checks whether the given location is inside this box in the XY plane.
        /// </summary>
        /// <param name="in">The location to test for inside the bounding box.</param>
        /// <returns>true if location is inside this box in the XY plane.</returns>
        public bool IsInsideXY(FVector @in) => (@in.X > Min.X) && (@in.X < Max.X) && (@in.Y > Min.Y) && (@in.Y < Max.Y);

        /// <summary>
        /// Checks whether the given box is fully encapsulated by this box in the XY plane.
        /// </summary>
        /// <param name="other">The box to test for encapsulation within the bounding box.</param>
        /// <returns>true if box is inside this box in the XY plane.</returns>
        public bool IsInsideXY(FBox other) => IsInsideXY(other.Min) && IsInsideXY(other.Max);

        public FBox TransformBy(FMatrix m)
        {
            // if we are not valid, return another invalid box.
            if (IsValid == 0)
            {
                return new FBox();
            }

            var vecMin = Min;
            var vecMax = Max;

            var m0 = new FVector(m.M00, m.M01, m.M02);
            var m1 = new FVector(m.M10, m.M11, m.M12);
            var m2 = new FVector(m.M20, m.M21, m.M22);
            var m3 = new FVector(m.M30, m.M31, m.M32);

            var half = new FVector(0.5f, 0.5f, 0.5f);
            var origin = (vecMax + vecMin) * half;
            var extent = (vecMax - vecMin) * half;

            var newOrigin = new FVector(origin.X) * m0 +
                            new FVector(origin.Y) * m1 +
                            new FVector(origin.Z) * m2 +
                            m3;

            var newExtent = (new FVector(extent.X) * m0).Abs() +
                            (new FVector(extent.Y) * m1).Abs() +
                            (new FVector(extent.Z) * m2).Abs();

            return new FBox
            {
                Min = newOrigin - newExtent,
                Max = newOrigin + newExtent,
                IsValid = 1
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FBox TransformBy(FTransform m) => TransformBy(m.ToMatrixWithScale());

        public override string ToString() => $"IsValid={IsValid != 0}, Min={Min}, Max={Max}";

        /// <summary>
        /// Utility function to build an AABB from Origin and Extent
        /// </summary>
        /// <param name="origin">The location of the bounding box.</param>
        /// <param name="extent">Half size of the bounding box.</param>
        /// <returns>A new axis-aligned bounding box.</returns>
        public static FBox BuildAABB(FVector origin, FVector extent) => new FBox(origin - extent, origin + extent);
    }
}
