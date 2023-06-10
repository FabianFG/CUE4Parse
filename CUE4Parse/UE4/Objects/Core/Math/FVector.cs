using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// USE Ar.Read<FVector> FOR FLOATS AND new FVector(Ar) FOR DOUBLES
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FVector : IUStruct
    {
        /// <summary>
        /// Allowed error for a normalized vector (against squared magnitude)
        /// </summary>
        public const float ThreshVectorNormalized = 0.01f;

        public static readonly FVector ZeroVector = new(0, 0, 0);
        public static readonly FVector OneVector = new(1, 1, 1);
        public static readonly FVector UpVector = new(0, 0, 1);
        public static readonly FVector ForwardVector = new(1, 0, 0);
        public static readonly FVector RightVector = new(0, 1, 0);

        public float X;
        public float Y;
        public float Z;

        /// <summary>
        /// Value to set all components to.
        /// </summary>
        /// <param name="x">X Coordinate.</param>
        /// <param name="y">Y Coordinate.</param>
        /// <param name="z">Z Coordinate.</param>
        public FVector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public FVector(double x, double y, double z)
        {
            X = (float) x;
            Y = (float) y;
            Z = (float) z;
        }

        public FVector(FArchive Ar)
        {
            if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
            {
                X = (float) Ar.Read<double>();
                Y = (float) Ar.Read<double>();
                Z = (float) Ar.Read<double>();
            }
            else
            {
                X = Ar.Read<float>();
                Y = Ar.Read<float>();
                Z = Ar.Read<float>();
            }
        }

        /// <summary>
        /// Constructor initializing all components to a single float value.
        /// </summary>
        /// <param name="f">Value to set all components to.</param>
        public FVector(float f) : this(f, f, f) { }

        /// <summary>
        /// Constructs a vector from an FVector2D and Z value.
        /// </summary>
        /// <param name="v">Vector to copy from.</param>
        /// <param name="z">Z Coordinate.</param>
        public FVector(FVector2D v, float z) : this(v.X, v.Y, z) { }

        /// <summary>
        /// Constructor using the XYZ components from a 4D vector.
        /// </summary>
        /// <param name="v">4D Vector to copy from.</param>
        public FVector(FVector4 v) : this(v.X, v.Y, v.Z) { }

        /// <summary>
        /// Constructs a vector from an FLinearColor.
        /// </summary>
        /// <param name="color">Color to copy from.</param>
        public FVector(FLinearColor color) : this(color.R, color.G, color.B) { }

        /// <summary>
        /// Constructs a vector from an FIntVector.
        /// </summary>
        /// <param name="v">FIntVector to copy from.</param>
        public FVector(FIntVector v) : this(v.X, v.Y, v.Z) { }

        /// <summary>
        /// Constructs a vector from an FIntPoint.
        /// </summary>
        /// <param name="p">Int Point used to set X and Y coordinates, Z is set to zero.</param>
        public FVector(FIntPoint p) : this(p.X, p.Y, 0f) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector Set(FVector other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector Set(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector GetSignVector() => new()
        {
            //FloatSelect: return Comparand >= 0.f ? ValueGEZero : ValueLTZero;
            X = X >= 0 ? 1 : -1, Y = Y >= 0 ? 1 : -1, Z = Z >= 0 ? 1 : -1
        };

        public void Scale(float scale)
        {
            X *= scale;
            Y *= scale;
            Z *= scale;
        }

        public void Scale(FVector scale)
        {
            X *= scale.X;
            Y *= scale.Y;
            Z *= scale.Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator +(FVector a) => a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator -(FVector a) => new(-a.X, -a.Y, -a.Z);

        /// <summary>
        /// Calculate cross product between this and another vector.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>The cross product</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator ^(FVector a, FVector b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );

        /// <summary>
        /// Calculate the dot product between this and another vector.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>The dot product</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float operator |(FVector a, FVector b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator +(FVector a, FVector b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator +(FVector a, float bias) => new(a.X + bias, a.Y + bias, a.Z + bias);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator -(FVector a, FVector b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator -(FVector a, float bias) => new(a.X - bias, a.Y - bias, a.Z - bias);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator *(FVector a, FVector b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator *(FVector a, float scale) => new(a.X * scale, a.Y * scale, a.Z * scale);

        public static FVector operator *(FVector v, FQuat q)
        {
            var u = new FVector(q.X, q.Y, q.Z);
            float s = q.W;

            return 2.0f * DotProduct(u, v) * u
                     + (s*s - DotProduct(u, u)) * v
                     + 2.0f * s * CrossProduct(u, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator *(float scale, FVector a) => a * scale;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator /(FVector a, FVector b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator /(FVector a, float scale)
        {
            var rScale = 1f / scale;
            return new FVector(a.X * rScale, a.Y * rScale, a.Z * rScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector operator /(float scale, FVector a) => a / scale;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FVector a, FVector b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FVector a, FVector b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

        public float this[int i]
        {
            get => i switch
            {
                0 => X,
                1 => Y,
                2 => Z,
                _ => throw new IndexOutOfRangeException()
            };
            set
            {
                switch (i)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public override bool Equals(object? obj) => obj is FVector other && Equals(other, 0f);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Check against another vector for equality, within specified error limits.
        /// </summary>
        /// <param name="v">The vector to check against.</param>
        /// <param name="tolerance">Error tolerance.</param>
        /// <returns>true if the vectors are equal within tolerance limits, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FVector v, float tolerance = UnrealMath.KindaSmallNumber) => MathF.Abs(X - v.X) <= tolerance && MathF.Abs(Y - v.Y) <= tolerance && MathF.Abs(Z - v.Z) <= tolerance;

        /// <summary>
        /// Checks whether all components of this vector are the same, within a tolerance.
        /// </summary>
        /// <param name="tolerance">Error tolerance.</param>
        /// <returns>true if the vectors are equal within tolerance limits, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllComponentsEqual(float tolerance = UnrealMath.KindaSmallNumber) => MathF.Abs(X - Y) <= tolerance && MathF.Abs(X - Z) <= tolerance && MathF.Abs(Y - Z) <= tolerance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Max() => MathF.Max(MathF.Max(X, Y), Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float AbsMax() => MathF.Max(MathF.Max(MathF.Abs(X), MathF.Abs(Y)), MathF.Abs(Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Min() => MathF.Min(MathF.Min(X, Y), Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float AbsMin() => MathF.Min(MathF.Min(MathF.Abs(X), MathF.Abs(Y)), MathF.Abs(Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector ComponentMax(FVector other) => new(MathF.Max(X, other.X), MathF.Max(Y, other.Y), MathF.Max(Z, other.Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector ComponentMin(FVector other) => new(MathF.Min(X, other.X), MathF.Min(Y, other.Y), MathF.Min(Z, other.Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector Abs() => new(MathF.Abs(X), MathF.Abs(Y), MathF.Abs(Z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Size() => MathF.Sqrt(X * X + Y * Y + Z * Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SizeSquared() => X * X + Y * Y + Z * Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Size2D() => MathF.Sqrt(X * X + Y * Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SizeSquared2D() => X * X + Y * Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsNaN() => !float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Z);

        /// <summary>
        /// Checks whether vector is near to zero within a specified tolerance.
        /// </summary>
        /// <param name="tolerance">Error tolerance.</param>
        /// <returns>true if the vector is near to zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNearlyZero(float tolerance = UnrealMath.KindaSmallNumber) => MathF.Abs(X) <= tolerance && MathF.Abs(Y) <= tolerance && MathF.Abs(Z) <= tolerance;

        /// <summary>
        /// Checks whether all components of the vector are exactly zero.
        /// </summary>
        /// <returns>true if the vector is exactly zero, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsZero() => X == 0 && Y == 0 && Z == 0;

        /// <summary>
        /// Check if the vector is of unit length, with specified tolerance.
        /// </summary>
        /// <param name="lengthSquaredTolerance">Tolerance against squared length.</param>
        /// <returns>true if the vector is a unit vector within the specified tolerance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnit(float lengthSquaredTolerance = UnrealMath.KindaSmallNumber) => MathF.Abs(1f - SizeSquared()) < lengthSquaredTolerance;

        /// <summary>
        /// Checks whether vector is normalized.
        /// </summary>
        /// <returns>true if normalized, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNormalized() => MathF.Abs(1f - SizeSquared()) < ThreshVectorNormalized;

        /// <summary>
        /// Normalize this vector in-place if it is larger than a given tolerance. Leaves it unchanged if not.
        /// </summary>
        /// <param name="tolerance">Minimum squared length of vector for normalization.</param>
        /// <returns>if the vector was normalized correctly, false otherwise.</returns>
        public bool Normalize(float tolerance = UnrealMath.SmallNumber)
        {
            var squareSum = X * X + Y * Y + Z * Z;
            if (squareSum > tolerance)
            {
                var scale = squareSum.InvSqrt();
                X *= scale;
                Y *= scale;
                Z *= scale;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create a copy of this vector, with its maximum magnitude clamped to MaxSize.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector GetClampedToMaxSize(float maxSize)
        {
            if (maxSize < UnrealMath.KindaSmallNumber)
            {
                return new FVector(0, 0, 0); // ZeroVector
            }

            var vSq = SizeSquared();
            if (vSq > maxSize * maxSize)
            {
                var scale = maxSize * vSq.InvSqrt();
                return new FVector(X * scale, Y * scale, Z * scale);
            }

            return new FVector(X, Y, Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector GetSafeNormal(float tolerance = UnrealMath.SmallNumber)
        {
            var squareSum = X * X + Y * Y + Z * Z;

            // Not sure if it's safe to add tolerance in there. Might introduce too many errors
            if (squareSum == 1.0f)
            {
                return this;
            }

            if (squareSum < tolerance)
            {
                return ZeroVector;
            }

            var scale = squareSum.InvSqrt();
            return new FVector(X * scale, Y * scale, Z * scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector GetSafeNormal2D(float tolerance = UnrealMath.SmallNumber)
        {
            var squareSum = X * X + Y * Y;

            // Not sure if it's safe to add tolerance in there. Might introduce too many errors
            if (squareSum == 1.0f)
            {
                return Z == 0.0f ? this : new FVector(X, Y, 0.0f);
            }

            if (squareSum < tolerance)
            {
                return ZeroVector;
            }

            var scale = squareSum.InvSqrt();
            return new FVector(X * scale, Y * scale, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CosineAngle2D(FVector b)
        {
            var a = this;
            a.Z = 0.0f;
            b.Z = 0.0f;
            a.Normalize();
            b.Normalize();
            return a | b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector ProjectOnTo(FVector a) => a * ((this | a) / (a | a));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector ProjectOnToNormal(FVector normal) => normal * (this | normal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FRotator ToOrientationRotator()
        {
            return new FRotator
            {
                // Find yaw.
                Yaw = MathF.Atan2(Y, X) * (180.0f / MathF.PI),
                // Find pitch.
                Pitch = MathF.Atan2(Z, MathF.Sqrt(X * X + Y * Y)) * (180.0f / MathF.PI),
                // Find roll.
                Roll = 0
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FQuat ToOrientationQuat()
        {
            // Essentially an optimized Vector->Rotator->Quat made possible by knowing Roll == 0, and avoiding radians->degrees->radians.
            // This is done to avoid adding any roll (which our API states as a constraint).
            var YawRad = MathF.Atan2(Y, X);
            var PitchRad = MathF.Atan2(Z, MathF.Sqrt(X * X + Y * Y));

            const float DIVIDE_BY_2 = 0.5f;
            float SP = MathF.Sin(PitchRad * DIVIDE_BY_2), SY = MathF.Sin(YawRad * DIVIDE_BY_2);
            float CP = MathF.Cos(PitchRad * DIVIDE_BY_2), CY = MathF.Cos(YawRad * DIVIDE_BY_2);

            return new FQuat
            {
                X = SP * SY,
                Y = -SP * CY,
                Z = CP * SY,
                W = CP * CY
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FRotator Rotation() => ToOrientationRotator();

        public override string ToString() => $"X={X,3:F3} Y={Y,3:F3} Z={Z,3:F3}";

        /// <summary>
        /// Calculate the cross product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The cross product</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector CrossProduct(FVector a, FVector b) => a ^ b;

        /// <summary>
        /// Calculate the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DotProduct(FVector a, FVector b) => a | b;

        /// <summary>
        /// Util to calculate distance from a point to a bounding box
        /// </summary>
        /// <param name="mins">3D Point defining the lower values of the axis of the bound box</param>
        /// <param name="maxs">3D Point defining the higher values of the axis of the bound box</param>
        /// <param name="point">3D position of interest</param>
        /// <returns>the distance from the Point to the bounding box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeSquaredDistanceFromBoxToPoint(FVector mins, FVector maxs, FVector point)
        {
            // Accumulates the distance as we iterate axis
            var distSquared = 0f;

            // Check each axis for min/max and add the distance accordingly
            // NOTE: Loop manually unrolled for > 2x speed up
            if (point.X < mins.X)
            {
                distSquared += (point.X - mins.X) * (point.X - mins.X);
            }
            else if (point.X > maxs.X)
            {
                distSquared += (point.X - maxs.X) * (point.X - maxs.X);
            }

            if (point.Y < mins.Y)
            {
                distSquared += (point.Y - mins.Y) * (point.Y - mins.Y);
            }
            else if (point.Y > maxs.Y)
            {
                distSquared += (point.Y - maxs.Y) * (point.Y - maxs.Y);
            }

            if (point.Z < mins.Z)
            {
                distSquared += (point.Z - mins.Z) * (point.Z - mins.Z);
            }
            else if (point.Z > maxs.Z)
            {
                distSquared += (point.Z - maxs.Z) * (point.Z - maxs.Z);
            }

            return distSquared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PointPlaneDist(FVector point, FVector planeBase, FVector planeNormal) => (point - planeBase) | planeNormal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector PointPlaneProject(FVector point, FVector planeBase, FVector planeNormal)
        {
            // Find the distance of X from the plane
            // Add the distance back along the normal from the point
            return point - PointPlaneDist(point, planeBase, planeNormal) * planeNormal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector VectorPlaneProject(FVector delta, FVector normal) => delta - delta.ProjectOnToNormal(normal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistSquared(FVector v1, FVector v2) => (v2.X - v1.X).Square() + (v2.Y - v1.Y).Square() + (v2.Z - v1.Z).Square();

        public void Serialize(FArchiveWriter Ar)
        {
            Ar.Write(X);
            Ar.Write(Y);
            Ar.Write(Z);
        }

        public static implicit operator Vector3(FVector v) => new(v.X, v.Y, v.Z);
    }
}
