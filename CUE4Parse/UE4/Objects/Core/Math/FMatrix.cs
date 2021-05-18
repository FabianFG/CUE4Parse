using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    public class FMatrix : IUStruct
    {
        public float M00;
        public float M01;
        public float M02;
        public float M03;
        public float M10;
        public float M11;
        public float M12;
        public float M13;
        public float M20;
        public float M21;
        public float M22;
        public float M23;
        public float M30;
        public float M31;
        public float M32;
        public float M33;
        
        public FMatrix() {}
        public FMatrix(FAssetArchive Ar)
        {
            M00 = Ar.Read<float>();
            M01 = Ar.Read<float>();
            M02 = Ar.Read<float>();
            M03 = Ar.Read<float>();
            M10 = Ar.Read<float>();
            M11 = Ar.Read<float>();
            M12 = Ar.Read<float>();
            M13 = Ar.Read<float>();
            M20 = Ar.Read<float>();
            M21 = Ar.Read<float>();
            M22 = Ar.Read<float>();
            M23 = Ar.Read<float>();
            M30 = Ar.Read<float>();
            M31 = Ar.Read<float>();
            M32 = Ar.Read<float>();
            M33 = Ar.Read<float>();
        }
        
        public FVector4 TransformFVector4(FVector4 p)
        {
            return new(
                p.X * M00 + p.Y * M10 + p.Z * M20 + p.W * M30,
                p.X * M01 + p.Y * M11 + p.Z * M21 + p.W * M31,
                p.X * M02 + p.Y * M12 + p.Z * M22 + p.W * M32,
                p.X * M03 + p.Y * M13 + p.Z * M23 + p.W * M33
            );
        }

        public FVector4 TransformFVector(FVector v)
        {
            return TransformFVector4(new FVector4(v.X, v.Y, v.Z, 0f));
        }

        public FMatrix GetTransposed()
        {
            return new()
            {
                M00 = M00,
                M01 = M10,
                M02 = M20,
                M03 = M30,
                M10 = M01,
                M11 = M11,
                M12 = M21,
                M13 = M31,
                M20 = M02,
                M21 = M12,
                M22 = M22,
                M23 = M32,
                M30 = M03,
                M31 = M13,
                M32 = M23,
                M33 = M33
            };
        }

        public FVector GetOrigin()
        {
            return new(M30, M31, M32);
        }

        public override string ToString()
        {
            return $"[{M00:F1} {M01:F1} {M02:F1} {M03:F1}] [{M10:F1} {M11:F1} {M12:F1} {M13:F1}] [{M20:F1} {M21:F1} {M22:F1} {M23:F1}] [{M30:F1} {M31:F1} {M32:F1} {M33:F1}]";
        }
    }
    
    public class FRotationTranslationMatrix : FMatrix
    {
        private const double PI = 3.14159265358979323846;
        
        public FRotationTranslationMatrix(FRotator rot, FVector origin)
        {
            var p = rot.Pitch / 180.0 * PI;
            var y = rot.Yaw / 180.0 * PI;
            var r = rot.Roll / 180.0 * PI;
            var sP = (float)System.Math.Sin(p);
            var sY = (float)System.Math.Sin(y);
            var sR = (float)System.Math.Sin(r);
            var cP = (float)System.Math.Cos(p);
            var cY = (float)System.Math.Cos(y);
            var cR = (float)System.Math.Cos(r);

            M00 = cP * cY;
            M01 = cP * sY;
            M02 = sP;
            M03 = 0f;

            M10 = sR * sP * cY - cR * sY;
            M11 = sR * sP * sY + cR * cY;
            M12 = -sR * cP;
            M13 = 0f;

            M20 = -(cR * sP * cY + sR * sY);
            M21 = cY * sR - cR * sP * sY;
            M22 = cR * cP;
            M23 = 0f;

            M30 = origin.X;
            M31 = origin.Y;
            M32 = origin.Z;
            M33 = 1f;
        }
    }
    
    public sealed class FRotationMatrix : FRotationTranslationMatrix
    {
        public FRotationMatrix(FRotator rot) : base(rot, new FVector()) { }
    }
}