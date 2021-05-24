using System;
using System.Runtime.CompilerServices;
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

        public bool IsRotationNormalized => Rotation.IsNormalized;

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

        public bool Equals(FTransform other, float tolerance = FVector.KindaSmallNumber)
        {
            return Rotation.Equals(other.Rotation, tolerance) && Translation.Equals(other.Translation, tolerance) && Scale3D.Equals(other.Scale3D, tolerance);
        }

        public bool ContainsNan()
        {
            return Translation.ContainsNaN() || Rotation.ContainsNaN() || Scale3D.ContainsNaN();
        }

        public static bool AnyHasNegativeScale(FVector scale3D, FVector otherScale3D) => scale3D.X < 0 || scale3D.Y < 0 || scale3D.Z < 0 || 
            otherScale3D.X < 0 || otherScale3D.Y < 0 || otherScale3D.Z < 0;

        public FTransform GetRelativeTransform(FTransform other)
        {
            // A * B(-1) = VQS(B)(-1) (VQS (A))
            // 
            // Scale = S(A)/S(B)
            // Rotation = Q(B)(-1) * Q(A)
            // Translation = 1/S(B) *[Q(B)(-1)*(T(A)-T(B))*Q(B)]
            // where A = this, B = Other
            var result = new FTransform();

            if (AnyHasNegativeScale(Scale3D, other.Scale3D))
            {
                // @note, if you have 0 scale with negative, you're going to lose rotation as it can't convert back to quat
                GetRelativeTransformUsingMatrixWithScale(ref result, ref this, ref other);
            }
            else
            {
                var safeRecipScale3D = GetSafeScaleReciprocal(other.Scale3D, FVector.SmallNumber);
                result.Scale3D = Scale3D * safeRecipScale3D;

                if (!other.Rotation.IsNormalized)
                    return Identity;

                var inverse = other.Rotation.Inverse();
                result.Rotation = inverse * Rotation;

                result.Translation = (inverse * (Translation - other.Translation)) * safeRecipScale3D;
            }

            return result;
        }

        public static void GetRelativeTransformUsingMatrixWithScale(ref FTransform outTransform, ref FTransform Base,
            ref FTransform Relative)
        {
            // the goal of using M is to get the correct orientation
            // but for translation, we still need scale
            var am = Base.ToMatrixWithScale();
            var bm = Base.ToMatrixWithScale();
            // get combined scale
            var safeRecipScale3D = GetSafeScaleReciprocal(Relative.Scale3D, FVector.SmallNumber);
            var desiredScale3D = Base.Scale3D * safeRecipScale3D;
            
        }

        public static void ConstructTransformFromMatrixWithDesiredScale(FMatrix aMatrix, FMatrix bMatrix,
            FVector desiredScale, ref FTransform outTransform)
        {
            // the goal of using M is to get the correct orientation
            // but for translation, we still need scale
            var m = aMatrix * bMatrix;
            m.RemoveScaling();
            
            // apply negative scale back to axes
            var signedScale = desiredScale.GetSignVector();
            
            m.SetAxis(0, m.GetScaledAxis(EAxis.X) * signedScale.X);
            m.SetAxis(1, m.GetScaledAxis(EAxis.Y) * signedScale.Y);
            m.SetAxis(2, m.GetScaledAxis(EAxis.Z) * signedScale.Z);
            
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
        public static FVector GetSafeScaleReciprocal(FVector scale, float tolerance)
        {
            var safeReciprocalScale = new FVector();
            if (Math.Abs(scale.X) <= tolerance)
                safeReciprocalScale.X = 0;
            else
                safeReciprocalScale.X = 1 / scale.X;
            
            if (Math.Abs(scale.Y) <= tolerance)
                safeReciprocalScale.Y = 0;
            else
                safeReciprocalScale.Y = 1 / scale.Y;
            
            if (Math.Abs(scale.Z) <= tolerance)
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

            var result = new FTransform();
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
        
    }
}
