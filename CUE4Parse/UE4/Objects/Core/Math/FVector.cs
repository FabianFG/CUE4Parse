using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FVector : IUStruct
    {
        /// <summary>
        /// Allowed error for a normalized vector (against squared magnitude) 
        /// </summary>
        public const float ThreshVectorNormalized = 0.01f;

        private const float SmallNumber = 1e-8f;
        private const float KindaSmallNumber = 1e-4f;
        
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

        public FVector Set(FVector other)
        {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
            return this;
        }
        public FVector Set(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            return this;
        }

        public static FVector operator +(FVector a) => a;
        public static FVector operator -(FVector a) => new FVector(-a.X, -a.Y, -a.Z);
        
        /// <summary>
        /// Calculate cross product between this and another vector.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>The cross product</returns>
        public static FVector operator ^(FVector a, FVector b) => new FVector(
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
        public static float operator |(FVector a, FVector b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        
        public static FVector operator +(FVector a, FVector b) => new FVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static FVector operator +(FVector a, float bias) => new FVector(a.X + bias, a.Y + bias, a.Z + bias);
        public static FVector operator -(FVector a, FVector b) => new FVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static FVector operator -(FVector a, float bias) => new FVector(a.X - bias, a.Y - bias, a.Z - bias);
        
        public static FVector operator *(FVector a, FVector b) => new FVector(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        public static FVector operator *(FVector a, float scale) => new FVector(a.X * scale, a.Y * scale, a.Z * scale);
        
        public static FVector operator /(FVector a, FVector b) => new FVector(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        public static FVector operator /(FVector a, float scale)
        {
            var rScale = 1f / scale;
            return new FVector(a.X * rScale, a.Y * rScale, a.Z * rScale);
        }

        public float this[int i]
        {
            get
            {
                return i switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException()
                };
            }
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

        public bool Equals(FVector other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object? obj)
        {
            return obj is FVector other && Equals(other);
        }

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
        public bool Equals(FVector v, float tolerance = KindaSmallNumber) => System.Math.Abs(X - v.X) <= tolerance &&
                                                                             System.Math.Abs(Y - v.Y) <= tolerance &&
                                                                             System.Math.Abs(Z - v.Z) <= tolerance;
        
        /// <summary>
        /// Checks whether all components of this vector are the same, within a tolerance.
        /// </summary>
        /// <param name="tolerance">Error tolerance.</param>
        /// <returns>true if the vectors are equal within tolerance limits, false otherwise.</returns>
        public bool AllComponentsEqual(float tolerance = KindaSmallNumber) => System.Math.Abs(X - Y) <= tolerance &&
                                                                             System.Math.Abs(X - Z) <= tolerance &&
                                                                             System.Math.Abs(Y - Z) <= tolerance;        

        public float Max() => System.Math.Max(System.Math.Max(X, Y), Z);
        public float AbsMax() => System.Math.Max(System.Math.Max(System.Math.Abs(X), System.Math.Abs(Y)), System.Math.Abs(Z));
        public float Min() => System.Math.Min(System.Math.Min(X, Y), Z);
        public float AbsMin() => System.Math.Min(System.Math.Min(System.Math.Abs(X), System.Math.Abs(Y)), System.Math.Abs(Z));
        public FVector ComponentMax(FVector other) => new FVector(System.Math.Max(X, other.X), System.Math.Max(Y, other.Y), System.Math.Max(Z, other.Z));
        public FVector ComponentMin(FVector other) => new FVector(System.Math.Min(X, other.X), System.Math.Min(Y, other.Y), System.Math.Min(Z, other.Z));
        public FVector Abs() => new FVector(System.Math.Abs(X), System.Math.Abs(Y), System.Math.Abs(Z));
        public double Size() => System.Math.Sqrt(X * X + Y * Y + Z * Z);
        public float SizeSquared() => X * X + Y * Y + Z * Z;
        public double Size2D() => System.Math.Sqrt(X * X + Y * Y);
        public float SizeSquared2D() => X * X + Y * Y;

        /// <summary>
        /// Checks whether vector is near to zero within a specified tolerance.
        /// </summary>
        /// <param name="tolerance">Error tolerance.</param>
        /// <returns>true if the vector is near to zero, false otherwise.</returns>
        public bool IsNearlyZero(float tolerance = KindaSmallNumber) => System.Math.Abs(X) <= tolerance &&
                                                                        System.Math.Abs(Y) <= tolerance &&
                                                                        System.Math.Abs(Z) <= tolerance;

        public bool IsZero() => X == 0 && Y == 0 && Z == 0;

        /// <summary>
        /// Check if the vector is of unit length, with specified tolerance.
        /// </summary>
        /// <param name="lengthSquaredTolerance">Tolerance against squared length.</param>
        /// <returns>true if the vector is a unit vector within the specified tolerance.</returns>
        public bool IsUnit(float lengthSquaredTolerance = KindaSmallNumber) =>
            System.Math.Abs(1f - SizeSquared()) < lengthSquaredTolerance;

        /// <summary>
        /// Checks whether vector is normalized.
        /// </summary>
        /// <returns>true if normalized, false otherwise.</returns>
        public bool IsNormalized() => System.Math.Abs(1f - SizeSquared()) < ThreshVectorNormalized;

        public override string ToString() => $"X={X:###.###} Y={Y:###.###} Z={Z:###.###}";

        /// <summary>
        /// Calculate the cross product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The cross product</returns>
        public static FVector CrossProduct(FVector a, FVector b) => a ^ b;
        
        /// <summary>
        /// Calculate the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>The dot product</returns>
        public static float DotProduct(FVector a, FVector b) => a | b;

        /// <summary>
        /// Util to calculate distance from a point to a bounding box
        /// </summary>
        /// <param name="mins">3D Point defining the lower values of the axis of the bound box</param>
        /// <param name="maxs">3D Point defining the higher values of the axis of the bound box</param>
        /// <param name="point">3D position of interest</param>
        /// <returns>the distance from the Point to the bounding box.</returns>
        public static float ComputeSquaredDistanceFromBoxToPoint(FVector mins, FVector maxs, FVector point)
        {
            // Accumulates the distance as we iterate axis
            var distSquared = 0f;
            
            // Check each axis for min/max and add the distance accordingly
            // NOTE: Loop manually unrolled for > 2x speed up
            if (point.X < mins.X)
            {
                distSquared += (point.X - mins.X) * (point.X - mins.X);
            } else if (point.X > maxs.X)
            {
                distSquared += (point.X - maxs.X) * (point.X - maxs.X);
            }

            if (point.Y < mins.Y)
            {
                distSquared += (point.Y - mins.Y) * (point.Y - mins.Y);
            } else if (point.Y > maxs.Y)
            {
                distSquared += (point.Y - maxs.Y) * (point.Y - maxs.Y);
            }

            if (point.Z < mins.Z)
            {
                distSquared += (point.Z - mins.Z) * (point.Z - mins.Z);
            } else if (point.Z > maxs.Z)
            {
                distSquared += (point.Z - maxs.Z) * (point.Z - maxs.Z);
            }

            return distSquared;
        }
    }
}