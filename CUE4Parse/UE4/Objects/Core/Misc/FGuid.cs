using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

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

    [JsonConverter(typeof(FGuidConverter))]
    [StructLayout(LayoutKind.Sequential)]
#pragma warning disable 660,661
    public struct FGuid : IUStruct
#pragma warning restore 660,661
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

        public FGuid(string hexString)
        {
            A = Convert.ToUInt32(hexString.Substring(0, 8), 16);
            B = Convert.ToUInt32(hexString.Substring(8, 8), 16);
            C = Convert.ToUInt32(hexString.Substring(16, 8), 16);
            D = Convert.ToUInt32(hexString.Substring(24, 8), 16);
        }

        public bool IsValid() => (A | B | C | D) != 0;

        public readonly string ToString(EGuidFormats guidFormat)
        {
            switch (guidFormat)
            {
                case EGuidFormats.DigitsWithHyphens: return
                    $"{A:X8}-{B >> 16:X4}-{B & 0xFFFF:X4}-{C >> 16:X4}-{C & 0xFFFF:X4}{D:X8}";
                case EGuidFormats.DigitsWithHyphensInBraces: return
                    $"{{{A:X8}-{B >> 16:X4}-{B & 0xFFFF:X4}-{C >> 16:X4}-{C & 0xFFFF:X4}{D:X8}}}";
                case EGuidFormats.DigitsWithHyphensInParentheses: return
                    $"({A:X8}-{B >> 16:X4}-{B & 0xFFFF:X4}-{C >> 16:X4}-{C & 0xFFFF:X4}{D:X8})";
                case EGuidFormats.HexValuesInBraces: return
                    $"{{0x{A:X8},0x{B >> 16:X4},0x{B & 0xFFFF:X4},{{0x{C >> 24:X2},0x{(C >> 16) & 0xFF:X2},0x{(C >> 8) & 0xFF:X2},0x{C & 0XFF:X2},0x{D >> 24:X2},0x{(D >> 16) & 0XFF:X2},0x{(D >> 8) & 0XFF:X2},0x{D & 0XFF:X2}}}}}";
                case EGuidFormats.UniqueObjectGuid: return $"{A:X8}-{B:X8}-{C:X8}-{D:X8}";
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
                        char[] alphabet = new char[36]
                        {
                            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
                            'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
                            'W', 'X', 'Y', 'Z'
                        };

                        FUInt128 zero = new (0);
                        FUInt128 value = new (A, B, C, D);
                        StringBuilder builder = new (26);
                        while (value.IsGreater(zero))
                        {
                            value = value.Divide(36, out var remainder);
                            builder.Insert(0, alphabet[remainder]);
                        }

                        for (var i = builder.Length; i < 25; i++)
                        {
                            builder.Insert(0, '0');
                        }

                        builder.Insert(0, 0);
                        // reverse ?
                        return builder.ToString();
                    }
                default: return $"{A:X8}{B:X8}{C:X8}{D:X8}";
            }
        }

        public override string ToString()
        {
            return ToString(EGuidFormats.Digits);
        }

        public static bool operator ==(FGuid one, FGuid two) => one.A == two.A && one.B == two.B && one.C == two.C && one.D == two.D;
        public static bool operator !=(FGuid one, FGuid two) => one.A != two.A || one.B != two.B || one.C != two.C || one.D != two.D;

        public static implicit operator FGuid(Guid g) => new(g.ToString().Replace("-", ""));
    }
}
