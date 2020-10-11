using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.Core.Misc
{
    public enum EGuidFormats
    {
		Digits, // "00000000000000000000000000000000"
		DigitsWithHyphens, // 00000000-0000-0000-0000-000000000000
		DigitsWithHyphensInBraces, // {00000000-0000-0000-0000-000000000000}
		DigitsWithHyphensInParentheses, // (00000000-0000-0000-0000-000000000000)
		HexValuesInBraces, // {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
		UniqueObjectGuid, // 00000000-00000000-00000000-00000000
		Short, // AQsMCQ0PAAUKCgQEBAgADQ
        Base36Encoded, // 1DPF6ARFCM4XH5RMWPU8TGR0J
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct FGuid : IUStruct
    {
        public readonly uint A;
        public readonly uint B;
        public readonly uint C;
        public readonly uint D;

        public FGuid(uint v)
        {
            A = B = C = D = v;
        }

        public FGuid(uint a, uint b, uint c, uint d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        public unsafe string HexString => UnsafePrint.BytesToHex(
            (byte*) Unsafe.AsPointer(ref this), 16);

        public bool IsValid() => (A | B | C | D) != 0;

        public string ToString(EGuidFormats guidFormat)
        {
            switch (guidFormat)
            {
                case EGuidFormats.DigitsWithHyphens: return string.Format("{0:X8}-{1:X4}-{2:X4}-{3:X4}-{4:X4}{5:X8}", A, B >> 16, B & 0xFFFF, C >> 16, C & 0xFFFF, D);
                case EGuidFormats.DigitsWithHyphensInBraces: return string.Format("{{{0:X8}-{1:X4}-{2:X4}-{3:X4}-{4:X4}{5:X8}}}", A, B >> 16, B & 0xFFFF, C >> 16, C & 0xFFFF, D);
                case EGuidFormats.DigitsWithHyphensInParentheses: return string.Format("({0:X8}-{1:X4}-{2:X4}-{3:X4}-{4:X4}{5:X8})", A, B >> 16, B & 0xFFFF, C >> 16, C & 0xFFFF, D);
                case EGuidFormats.HexValuesInBraces: return string.Format("{{0x{0:X8},0x{1:X4},0x{2:X4},{{0x{3:X2},0x{4:X2},0x{5:X2},0x{6:X2},0x{7:X2},0x{8:X2},0x{9:X2},0x{10:X2}}}}}", A, B >> 16, B & 0xFFFF, C >> 24, (C >> 16) & 0xFF, (C >> 8) & 0xFF, C & 0XFF, D >> 24, (D >> 16) & 0XFF, (D >> 8) & 0XFF, D & 0XFF);
                case EGuidFormats.UniqueObjectGuid: return string.Format("{0:X8}-{1:X8}-{2:X8}-{3:X8}", A, B, C, D);
                case EGuidFormats.Short:
                    {
                        IEnumerable<byte> data = BitConverter.GetBytes(A).Concat(BitConverter.GetBytes(B)).Concat(BitConverter.GetBytes(C)).Concat(BitConverter.GetBytes(D));
                        string result = Convert.ToBase64String(data.ToArray()).Replace('+', '-').Replace('/', '_');
                        if (result.Length == 24) // Remove trailing '=' base64 padding
                            result = result.Substring(0, result.Length - 2);

                        return result;
                    }
                case EGuidFormats.Base36Encoded: // if this doesn't work, i'm not surprised
                    {
                        char[] Alphabet = new char[36]
                        {
                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
                            'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
                            'W', 'X', 'Y', 'Z'
                        };

                        FUInt128 zero = new FUInt128(0);
                        FUInt128 value = new FUInt128(A, B, C, D);
                        StringBuilder builder = new StringBuilder(26);
                        while (value.IsGreater(zero))
                        {
                            value = value.Divide(36, out uint remainder);
                            builder.Insert(0, Alphabet[remainder]);
                        }

                        for (int i = builder.Length; i < 25; i++)
                        {
                            builder.Insert(0, '0');
                        }

                        builder.Insert(0, 0);
                        // reverse ?
                        return builder.ToString();
                    }
                default: return string.Format("{0:X8}{1:X8}{2:X8}{3:X8}", A, B, C, D);
            }
        }

        public override string ToString()
        {
            return ToString(EGuidFormats.Digits);
        }

        public static bool operator ==(FGuid one, FGuid two) => one.A == two.A && one.B == two.B && one.C == two.C && one.D == two.D;
        public static bool operator !=(FGuid one, FGuid two) => one.A != two.A || one.B != two.B || one.C != two.C || one.D != two.D;
    }
}