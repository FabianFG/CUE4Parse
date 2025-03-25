using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
    }

    [JsonConverter(typeof(FGuidConverter))]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FGuid : IUStruct, IEquatable<FGuid>
    {
        public const int Size = sizeof(uint) * 4;

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

        public FGuid(string hexString) : this(hexString.AsSpan()) { }

        public FGuid(ReadOnlySpan<char> hexString)
        {
            A = uint.Parse(hexString.Slice(0, 8), NumberStyles.HexNumber);
            B = uint.Parse(hexString.Slice(8, 8), NumberStyles.HexNumber);
            C = uint.Parse(hexString.Slice(16, 8), NumberStyles.HexNumber);
            D = uint.Parse(hexString.Slice(24, 8), NumberStyles.HexNumber);
        }

        public static FGuid Random()
        {
            Unsafe.SkipInit(out FGuid result);
            RandomNumberGenerator.Fill(result.GetSpan());
            return result;
        }

        public bool IsValid() => (A | B | C | D) != 0;

        public ReadOnlySpan<byte> AsByteSpan() =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<uint, byte>(ref Unsafe.AsRef(in A)), Size);

        public ReadOnlySpan<uint> AsSpan() =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in A), 4);

        internal Span<byte> GetSpan() =>
            MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref Unsafe.AsRef(in A)), Size);

        public string ToString(EGuidFormats guidFormat)
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
                        var data = AsByteSpan();
                        string result = Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_');
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

        public static implicit operator FGuid(Guid g) => new(g.ToString().Replace("-", ""));

        public override string ToString()
        {
            return ToString(EGuidFormats.Digits);
        }

        public bool Equals(FGuid other)
        {
            return A == other.A && B == other.B && C == other.C && D == other.D;
        }

        public override bool Equals(object? obj)
        {
            return obj is FGuid other && Equals(other);
        }

        public override int GetHashCode() => HashCode.Combine(A, B, C, D);

        public static bool operator ==(FGuid left, FGuid right) => left.Equals(right);

        public static bool operator !=(FGuid left, FGuid right) => !left.Equals(right);
    }
}
