namespace CUE4Parse.UE4.Objects.Core.Math
{
    /** Rotation and translation matrix using quaternion rotation */
    public class FQuatRotationTranslationMatrix : FMatrix
    {
        public FQuatRotationTranslationMatrix(FQuat q, FVector origin)
        {
            var x2 = q.X + q.X;  var y2 = q.Y + q.Y;  var z2 = q.Z + q.Z;
            var xx = q.X * x2;   var xy = q.X * y2;   var xz = q.X * z2;
            var yy = q.Y * y2;   var yz = q.Y * z2;   var zz = q.Z * z2;
            var wx = q.W * x2;   var wy = q.W * y2;   var wz = q.W * z2;

            M00 = 1.0f - (yy + zz);	M10 = xy - wz;			M20 = xz + wy;			M30 = origin.X;
            M01 = xy + wz;			M11 = 1.0f - (xx + zz);	M21 = yz - wx;			M31 = origin.Y;
            M02 = xz - wy;			M12 = yz + wx;			M22 = 1.0f - (xx + yy);	M32 = origin.Z;
            M03 = 0.0f;				M13 = 0.0f;				M23 = 0.0f;				M33 = 1.0f;
        }
    }

    /** Rotation matrix using quaternion rotation */
    public class FQuatRotationMatrix : FQuatRotationTranslationMatrix
    {
        public FQuatRotationMatrix(FQuat q) : base(q, FVector.ZeroVector) { }
    }
}