using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public static class UnrealMathSSE
    {
        public static readonly Vector128<float> QMULTI_SIGN_MASK0 = Vector128.Create(1f, -1f, 1f, -1f);
        public static readonly Vector128<float> QMULTI_SIGN_MASK1 = Vector128.Create(1f, 1f, -1f, -1f);
        public static readonly Vector128<float> QMULTI_SIGN_MASK2 = Vector128.Create(-1f, 1f, 1f, -1f);

        public static byte ShuffleMask(byte A0, byte A1, byte B2, byte B3)
        {
            return (byte) (A0 | (A1 << 2) | (B2 << 4) | (B3 << 6));
        }

        // public static Vector128<float> MakeVectorRegister(float x, float y, float z, float w)
        // {
        //     return Vector128.Create(x, y, z, w);
        // }

        public static Vector128<float> VectorReplicate(Vector128<float> vec, byte elementIndex)
        {
            return Sse.Shuffle(vec, vec, ShuffleMask(elementIndex, elementIndex, elementIndex, elementIndex));
        }

        public static Vector128<float> VectorMultiply(Vector128<float> vec1, Vector128<float> vec2)
        {
            return Sse.Multiply(vec1, vec2);
        }

        public static Vector128<float> VectorSwizzle(Vector128<float> vec, byte x, byte y, byte z, byte w)
        {
            return Sse.Shuffle(vec, vec, ShuffleMask(x, y, z, w));
        }

        public static Vector128<float> VectorMultiplyAdd(Vector128<float> vec1, Vector128<float> vec2, Vector128<float> vec3)
        {
            return Sse.Add(Sse.Multiply(vec1, vec2), vec3);
        }

        public static FQuat VectorQuaternionMultiply2(FQuat quat1, FQuat quat2)
        {
            var vec1 = FQuat.AsVector128(quat1);
            var vec2 = FQuat.AsVector128(quat2);

            var r = VectorMultiply(VectorReplicate(vec1, 3), vec2);
            r = VectorMultiplyAdd(VectorMultiply(VectorReplicate(vec1, 0), VectorSwizzle(vec2, 3, 2, 1, 0)), QMULTI_SIGN_MASK0, r);
            r = VectorMultiplyAdd(VectorMultiply(VectorReplicate(vec1, 1), VectorSwizzle(vec2, 2, 3, 0, 1)), QMULTI_SIGN_MASK1, r);
            r = VectorMultiplyAdd(VectorMultiply(VectorReplicate(vec1, 2), VectorSwizzle(vec2, 1, 0, 3, 2)), QMULTI_SIGN_MASK2, r);
            var vec = r.AsVector4();
            return new FQuat(vec.X, vec.Y, vec.Z, vec.W);
        }
    }
}

