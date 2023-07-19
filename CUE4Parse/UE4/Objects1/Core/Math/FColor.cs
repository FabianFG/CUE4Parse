using System;
using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Math
{
    /// <summary>
    /// Stores a color with 8 bits of precision per channel.
    ///
    /// Note: Linear color values should always be converted to gamma space before stored in an FColor, as 8 bits of precision is not enough to store linear space colors!
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FColor : IUStruct
    {
        public readonly byte B;
        public readonly byte G;
        public readonly byte R;
        public readonly byte A;

        public string Hex => A is 1 or 0 ? UnsafePrint.BytesToHex(R, G, B) : UnsafePrint.BytesToHex(A, R, G, B);

        public FColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public void Serialize(FArchiveWriter Ar)
        {
            Ar.Write(R);
            Ar.Write(G);
            Ar.Write(B);
            Ar.Write(A);
        }

        public static implicit operator Vector4(FColor color) => new (
            Convert.ToSingle(color.R) / 255f, Convert.ToSingle(color.G) / 255f,
            Convert.ToSingle(color.B) / 255f, Convert.ToSingle(color.A) / 255f);

        public override string ToString() => Hex;

        public static byte Requantize16to8(int value16)
        {
            if (value16 is < 0 or > 65535)
            {
                throw new ArgumentException(nameof(value16));
            }

            // Dequantize x from 16 bit (Value16/65535.f)
            // then requantize to 8 bit with rounding (GPU convention UNorm)

            // matches exactly with :
            //  (int)( (Value16/65535.f) * 255.f + 0.5f );
            var value8 = (value16 * 255 + 32895) >> 16;
            return (byte) value8;
        }

        public int ToPackedARGB() => A << 24 + R << 16 + G << 8 + B;
    }
}
