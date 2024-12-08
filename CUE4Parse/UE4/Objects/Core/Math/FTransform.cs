using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Readers;
using static System.MathF;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    [StructFallback]
    public struct FTransform : IUStruct, ICloneable
    {
        public static FTransform Identity = new() { Rotation = FQuat.Identity, Translation = FVector.ZeroVector, Scale3D = FVector.OneVector };

        public FQuat Rotation;
        public FVector Translation;
        public FVector Scale3D;

        public bool IsRotationNormalized => Rotation.IsNormalized;

        public FTransform(EForceInit init = EForceInit.ForceInit)
        {
            Rotation = new FQuat(0f, 0f, 0f, 1f);
            Translation = new FVector(0f);
            Scale3D = FVector.OneVector;
        }

        public FTransform(FArchive Ar)
        {
            Rotation = new FQuat(Ar);
            Translation = new FVector(Ar);
            Scale3D = new FVector(Ar);
        }

        public FTransform(FVector translation)
        {
            Rotation = FQuat.Identity;
            Translation = translation;
            Scale3D = FVector.OneVector;
        }

        public FTransform(FQuat rotation)
        {
            Rotation = rotation;
            Translation = FVector.ZeroVector;
            Scale3D = FVector.OneVector;
        }

        public FTransform(FRotator rotation)
        {
            Rotation = new FQuat(rotation);
            Translation = FVector.ZeroVector;
            Scale3D = FVector.OneVector;
        }

        public FTransform(FQuat rotation, FVector translation, FVector scale3D)
        {
            Rotation = rotation;
            Translation = translation;
            Scale3D = scale3D;
        }

        public FTransform(FRotator rotation, FVector translation, FVector scale3D)
        {
            Rotation = new FQuat(rotation);
            Translation = translation;
            Scale3D = scale3D;
        }

        public FTransform(FStructFallback data)
        {
            Rotation = data.GetOrDefault<FQuat>(nameof(Rotation));
            Translation = data.GetOrDefault<FVector>(nameof(Translation));
            Scale3D = data.GetOrDefault<FVector>(nameof(Scale3D));
        }

        public void SetFromMatrix(FMatrix inMatrix)
        {
            FMatrix m = new(inMatrix);

            // Get the 3D scale from the matrix
            Scale3D = m.ExtractScaling();

            // If there is negative scaling going on, we handle that here
            if (inMatrix.Determinant() < 0.0f)
            {
                // Assume it is along X and modify transform accordingly.
                // It doesn't actually matter which axis we choose, the 'appearance' will be the same
                Scale3D.X *= -1.0f;
                m.SetAxis(0, -m.GetScaledAxis(EAxis.X));
            }

            Rotation = m.ToQuat();
            Translation = inMatrix.GetOrigin();

            // Normalize rotation
            Rotation.Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FRotator Rotator() => Rotation.Rotator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDeterminant() => Scale3D.X * Scale3D.Y * Scale3D.Z;

        public bool Equals(FTransform other, float tolerance = UnrealMath.KindaSmallNumber) =>
            Rotation.Equals(other.Rotation, tolerance) &&
            Translation.Equals(other.Translation, tolerance) &&
            Scale3D.Equals(other.Scale3D, tolerance);

        public bool ContainsNaN() => Translation.ContainsNaN() || Rotation.ContainsNaN() || Scale3D.ContainsNaN();

        public static bool AnyHasNegativeScale(FVector scale3D, FVector otherScale3D) =>
            scale3D.X < 0 || scale3D.Y < 0 || scale3D.Z < 0 ||
            otherScale3D.X < 0 || otherScale3D.Y < 0 || otherScale3D.Z < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScaleTranslation(FVector scale3D)
        {
            Translation *= scale3D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScaleTranslation(float scale)
        {
            Translation *= scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveScaling(float tolerance = UnrealMath.SmallNumber)
        {
            Scale3D = new FVector(1.0f, 1.0f, 1.0f);
            Rotation.Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMaximumAxisScale() => Scale3D.AbsMax();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMinimumAxisScale() => Scale3D.AbsMin();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTranslation(ref FTransform other)
        {
            Translation = other.Translation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyRotation(ref FTransform other)
        {
            Rotation = other.Rotation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyScale3D(ref FTransform other)
        {
            Scale3D = other.Scale3D;
        }

        public FTransform Inverse()
        {
            var invRotation = Rotation.Inverse();
            // this used to cause NaN if Scale contained 0
            var invScale3D = GetSafeScaleReciprocal(Scale3D);
            var invTranslation = invRotation * (invScale3D * -Translation);

            return new FTransform(invRotation, invTranslation, invScale3D);
        }

        public FTransform GetRelativeTransform(FTransform other)
        {
            // A * B(-1) = VQS(B)(-1) (VQS (A))
            //
            // Scale = S(A)/S(B)
            // Rotation = Q(B)(-1) * Q(A)
            // Translation = 1/S(B) *[Q(B)(-1)*(T(A)-T(B))*Q(B)]
            // where A = this, B = Other
            var result = new FTransform(EForceInit.ForceInit);

            if (AnyHasNegativeScale(Scale3D, other.Scale3D))
            {
                // @note, if you have 0 scale with negative, you're going to lose rotation as it can't convert back to quat
                GetRelativeTransformUsingMatrixWithScale(ref result, ref other);
            }
            else
            {
                var safeRecipScale3D = GetSafeScaleReciprocal(other.Scale3D, UnrealMath.SmallNumber);
                result.Scale3D = Scale3D * safeRecipScale3D;

                if (!other.Rotation.IsNormalized)
                    return Identity;

                var inverse = other.Rotation.Inverse();
                result.Rotation = inverse * Rotation;

                result.Translation = (inverse * (Translation - other.Translation)) * safeRecipScale3D;
            }

            return result;
        }

        public static FVector SubstractTranslations(FTransform a, FTransform b) => a.Translation - b.Translation;

        public void GetRelativeTransformUsingMatrixWithScale(ref FTransform outTransform, ref FTransform relative)
        {
            // the goal of using M is to get the correct orientation
            // but for translation, we still need scale
            var am = ToMatrixWithScale();
            var bm = ToMatrixWithScale();
            // get combined scale
            var safeRecipScale3D = GetSafeScaleReciprocal(relative.Scale3D, UnrealMath.SmallNumber);
            var desiredScale3D = Scale3D * safeRecipScale3D;
            ConstructTransformFromMatrixWithDesiredScale(am, bm.InverseFast(), desiredScale3D, ref outTransform);
        }

        public static void ConstructTransformFromMatrixWithDesiredScale(FMatrix aMatrix, FMatrix bMatrix, FVector desiredScale, ref FTransform outTransform)
        {
            // the goal of using M is to get the correct orientation
            // but for translation, we still need scale
            var m = aMatrix * bMatrix;
            m.RemoveScaling();

            // apply negative scale back to axes
            var signedScale = desiredScale.GetSignVector();

            m.SetAxis(0, signedScale.X * m.GetScaledAxis(EAxis.X));
            m.SetAxis(1, signedScale.Y * m.GetScaledAxis(EAxis.Y));
            m.SetAxis(2, signedScale.Z * m.GetScaledAxis(EAxis.Z));

            // @note: if you have negative with 0 scale, this will return rotation that is identity
            // since matrix loses that axes
            var rotation = new FQuat(m);
            rotation.Normalize();

            // set values back to output
            outTransform.Scale3D = desiredScale;
            outTransform.Rotation = rotation;

            // technically I could calculate this using FTransform but then it does more quat multiplication
            // instead of using Scale in matrix multiplication
            // it's a question of between RemoveScaling vs using FTransform to move translation
            outTransform.Translation = m.GetOrigin();
        }

        /**
	     * Convert this Transform to a transformation matrix with scaling.
	     */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FMatrix ToMatrixWithScale()
        {
            var outMatrix = new FMatrix();

            outMatrix.M30 = Translation.X;
            outMatrix.M31 = Translation.Y;
            outMatrix.M32 = Translation.Z;

            var x2 = Rotation.X + Rotation.X;
            var y2 = Rotation.Y + Rotation.Y;
            var z2 = Rotation.Z + Rotation.Z;
            {
                var xx2 = Rotation.X * x2;
                var yy2 = Rotation.Y * y2;
                var zz2 = Rotation.Z * z2;

                outMatrix.M00 = (1.0f - (yy2 + zz2)) * Scale3D.X;
                outMatrix.M11 = (1.0f - (xx2 + zz2)) * Scale3D.Y;
                outMatrix.M22 = (1.0f - (xx2 + yy2)) * Scale3D.Z;
            }
            {
                var yz2 = Rotation.Y * z2;
                var wx2 = Rotation.W * x2;

                outMatrix.M21 = (yz2 - wx2) * Scale3D.Z;
                outMatrix.M12 = (yz2 + wx2) * Scale3D.Y;
            }
            {
                var xy2 = Rotation.X * y2;
                var wz2 = Rotation.W * z2;

                outMatrix.M10 = (xy2 - wz2) * Scale3D.Y;
                outMatrix.M01 = (xy2 + wz2) * Scale3D.X;
            }
            {
                var xz2 = Rotation.X * z2;
                var wy2 = Rotation.W * y2;

                outMatrix.M20 = (xz2 + wy2) * Scale3D.Z;
                outMatrix.M02 = (xz2 - wy2) * Scale3D.X;
            }

            outMatrix.M03 = 0.0f;
            outMatrix.M13 = 0.0f;
            outMatrix.M23 = 0.0f;
            outMatrix.M33 = 1.0f;

            return outMatrix;
        }

        // mathematically if you have 0 scale, it should be infinite,
        // however, in practice if you have 0 scale, and relative transform doesn't make much sense
        // anymore because you should be instead of showing gigantic infinite mesh
        // also returning BIG_NUMBER causes sequential NaN issues by multiplying
        // so we hardcode as 0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FVector GetSafeScaleReciprocal(FVector scale, float tolerance = UnrealMath.SmallNumber)
        {
            var safeReciprocalScale = new FVector();
            if (Abs(scale.X) <= tolerance)
                safeReciprocalScale.X = 0;
            else
                safeReciprocalScale.X = 1 / scale.X;

            if (Abs(scale.Y) <= tolerance)
                safeReciprocalScale.Y = 0;
            else
                safeReciprocalScale.Y = 1 / scale.Y;

            if (Abs(scale.Z) <= tolerance)
                safeReciprocalScale.Z = 0;
            else
                safeReciprocalScale.Z = 1 / scale.Z;

            return safeReciprocalScale;
        }

        /** Returns Multiplied Transform of 2 FTransforms **/
        public static FTransform operator *(FTransform a, FTransform b)
        {
            if (!a.IsRotationNormalized) throw new ArgumentException("Rotation a must be normalized for multiplication");
            if (!b.IsRotationNormalized) throw new ArgumentException("Rotation b must be normalized for multiplication");

            //	When Q = quaternion, S = single scalar scale, and T = translation
            //	QST(A) = Q(A), S(A), T(A), and QST(B) = Q(B), S(B), T(B)

            //	QST (AxB)

            // QST(A) = Q(A)*S(A)*P*-Q(A) + T(A)
            // QST(AxB) = Q(B)*S(B)*QST(A)*-Q(B) + T(B)
            // QST(AxB) = Q(B)*S(B)*[Q(A)*S(A)*P*-Q(A) + T(A)]*-Q(B) + T(B)
            // QST(AxB) = Q(B)*S(B)*Q(A)*S(A)*P*-Q(A)*-Q(B) + Q(B)*S(B)*T(A)*-Q(B) + T(B)
            // QST(AxB) = [Q(B)*Q(A)]*[S(B)*S(A)]*P*-[Q(B)*Q(A)] + Q(B)*S(B)*T(A)*-Q(B) + T(B)

            //	Q(AxB) = Q(B)*Q(A)
            //	S(AxB) = S(A)*S(B)
            //	T(AxB) = Q(B)*S(B)*T(A)*-Q(B) + T(B)

            var result = new FTransform(EForceInit.ForceInit);
            if (AnyHasNegativeScale(a.Scale3D, b.Scale3D))
            {
                // @note, if you have 0 scale with negative, you're going to lose rotation as it can't convert back to quat
                MultiplyUsingMatrixWithScale(ref result, ref a, ref b);
            }
            else
            {
                result.Rotation = b.Rotation * a.Rotation;
                result.Scale3D = b.Scale3D * a.Scale3D;
                result.Translation = b.Rotation * (b.Scale3D * a.Translation) + b.Translation;
            }

            return result;
        }

        public static void MultiplyUsingMatrixWithScale(ref FTransform outTransform, ref FTransform a, ref FTransform b)
        {
            // the goal of using M is to get the correct orientation
            // but for translation, we still need scale
            ConstructTransformFromMatrixWithDesiredScale(a.ToMatrixWithScale(), b.ToMatrixWithScale(), a.Scale3D * b.Scale3D, ref outTransform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector TransformPosition(FVector v) => Rotation.RotateVector(Scale3D * v) + Translation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector TransformPositionNoScale(FVector v) => Rotation.RotateVector(v) + Translation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector InverseTransformPosition(FVector v) => Rotation.UnrotateVector(v - Translation) * GetSafeScaleReciprocal(Scale3D);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector InverseTransformPositionNoScale(FVector v) => Rotation.UnrotateVector(v - Translation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector TransformVector(FVector v) => Rotation.RotateVector(Scale3D * v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector TransformVectorNoScale(FVector v) => Rotation.RotateVector(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FQuat TransformRotation(FQuat q) => Rotation * q;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FQuat InverseTransformRotation(FQuat q) => Rotation.Inverse() * q;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FTransform GetScaled(float scale)
        {
            var a = this;
            a.Scale3D *= scale;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FTransform GetScaled(FVector scale)
        {
            var a = this;
            a.Scale3D *= scale;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector GetScaledAxis(EAxis axis) => axis switch
        {
            EAxis.X => TransformVector(new FVector(1.0f, 0.0f, 0.0f)),
            EAxis.Y => TransformVector(new FVector(0.0f, 1.0f, 0.0f)),
            _ => TransformVector(new FVector(0.0f, 0.0f, 1.0f))
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FVector GetUnitAxis(EAxis axis) => axis switch
        {
            EAxis.X => TransformVectorNoScale(new FVector(1.0f, 0.0f, 0.0f)),
            EAxis.Y => TransformVectorNoScale(new FVector(0.0f, 1.0f, 0.0f)),
            _ => TransformVectorNoScale(new FVector(0.0f, 0.0f, 1.0f))
        };

        public override string ToString()
        {
            return $"{{T:{Translation} R:{Rotation} S:{Scale3D}}}";
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
