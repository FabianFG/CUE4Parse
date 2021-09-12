using System;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /** Combined rotation and translation matrix */
    public class FRotationTranslationMatrix : FMatrix
    {
        public FRotationTranslationMatrix(FRotator rot, FVector origin)
        {
            var p = rot.Pitch / 180.0f * MathF.PI;
            var y = rot.Yaw / 180.0f * MathF.PI;
            var r = rot.Roll / 180.0f * MathF.PI;
            var sP = MathF.Sin(p);
            var sY = MathF.Sin(y);
            var sR = MathF.Sin(r);
            var cP = MathF.Cos(p);
            var cY = MathF.Cos(y);
            var cR = MathF.Cos(r);

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
}