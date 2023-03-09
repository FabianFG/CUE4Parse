using System;
using System.Runtime.CompilerServices;

namespace CUE4Parse.Utils
{
    // https://github.com/google/cityhash
    public static unsafe class CityHash
    {
        private const ulong K0 = 0xc3a5c85c97cb3127;
        private const ulong K1 = 0xb492b66fbe98f273;
        private const ulong K2 = 0x9ae16a3b2f90404f;

        public static ulong CityHash64(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var len = (uint) buffer.Length;

            fixed (byte* pBuffer = buffer)
            {
                var s = pBuffer;

                switch (len)
                {
                    case <= 32:
                        return len <= 16 ? HashLen0to16(s, len) : HashLen17to32(s, len);
                    case <= 64:
                        return HashLen33to64(s, len);
                }

                var x = Fetch64(s + len - 40);
                var y = Fetch64(s + len - 16) + Fetch64(s + len - 56);
                var z = HashLen16(Fetch64(s + len - 48) + len, Fetch64(s + len - 24));
                var v = WeakHashLen32WithSeeds(s + len - 64, len, z);
                var w = WeakHashLen32WithSeeds(s + len - 32, y + K1, x);
                x = x * K1 + Fetch64(s);
                len = (uint) (len - 1 & ~63);

                do
                {
                    x = Rotate(x + y + v.Item1 + Fetch64(s + 8), 37) * K1;
                    y = Rotate(y + v.Item2 + Fetch64(s + 48), 42) * K1;
                    x ^= w.Item2;
                    y += v.Item1 + Fetch64(s + 40);
                    z = Rotate(z + w.Item1, 33) * K1;
                    v = WeakHashLen32WithSeeds(s, v.Item2 * K1, x + w.Item1);
                    w = WeakHashLen32WithSeeds(s + 32, z + w.Item2, y + Fetch64(s + 16));
                    {
                        //swap z and x
                        z ^= x;
                        x ^= z;
                        z ^= x;
                    }
                    s += 64;
                    len -= 64;
                }
                while (len != 0);

                return HashLen16(HashLen16(v.Item1, w.Item1) + ShiftMix(y) * K1 + z, HashLen16(v.Item2, w.Item2) + x);
            }
        }

        private static ulong HashLen0to16(byte* s, uint len)
        {
            if (len >= 8)
            {
                ulong mul = K2 + len * 2;
                ulong a = Fetch64(s) + K2;
                ulong b = Fetch64(s + len - 8);
                ulong c = Rotate(b, 37) * mul + a;
                ulong d = (Rotate(a, 25) + b) * mul;
                return HashLen16(c, d, mul);
            }
            
            if (len >= 4)
            {
                ulong mul = K2 + len * 2;
                ulong a = Fetch32(s);
                return HashLen16(len + (a << 3), Fetch32(s + len - 4), mul);
            }
            
            if (len > 0)
            {
                byte a = s[0];
                byte b = s[len >> 1];
                byte c = s[len - 1];
                uint y = (uint)a + ((uint)b << 8);
                uint z = len + ((uint)c << 2);
                return ShiftMix(y * K2 ^ z * K0) * K2;
            }
            
            return K2;
        }

        private static ulong HashLen17to32(byte* s, uint len)
        {
            var mul = K2 + len * 2;
            var a = Fetch64(s) * K1;
            var b = Fetch64(s + 8);
            var c = Fetch64(s + len - 8) * mul;
            var d = Fetch64(s + len - 16) * K2;
            return HashLen16(Rotate(a + b, 43) + Rotate(c, 30) + d, a + Rotate(b + K2, 18) + c, mul);
        }

        private static ulong HashLen33to64(byte* s, uint len)
        {
            var mul = K2 + len * 2;
            var a = Fetch64(s) * K2;
            var b = Fetch64(s + 8);
            var c = Fetch64(s + len - 24);
            var d = Fetch64(s + len - 32);
            var e = Fetch64(s + 16) * K2;
            var f = Fetch64(s + 24) * 9;
            var g = Fetch64(s + len - 8);
            var h = Fetch64(s + len - 16) * mul;
            var u = Rotate(a + g, 43) + (Rotate(b, 30) + c) * 9;
            var v = (a + g ^ d) + f + 1;
            var w = Bswap_64((u + v) * mul) + h;
            var x = Rotate(e + f, 42) + c;
            var y = (Bswap_64((v + w) * mul) + g) * mul;
            var z = e + f + c;
            a = Bswap_64((x + z) * mul + y) + b;
            b = ShiftMix((z + a) * mul + d + h) * mul;
            return b + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Fetch32(byte* p)
        {
            return *(uint*) p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Fetch64(byte* p)
        {
            return *(ulong*) p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotate(ulong val, int shift)
        {
            return shift == 0 ? val : val >> shift | val << 64 - shift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ShiftMix(ulong val)
        {
            return val ^ val >> 47;
        }

        private static ulong HashLen16(ulong u, ulong v, ulong mul)
        {
            var a = (u ^ v) * mul;
            a ^= a >> 47;
            var b = (v ^ a) * mul;
            b ^= b >> 47;
            b *= mul;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Bswap_64(ulong value)
        {
            value = value << 8 & 0xFF00FF00FF00FF00 | value >> 8 & 0x00FF00FF00FF00FF;
            value = value << 16 & 0xFFFF0000FFFF0000 | value >> 16 & 0x0000FFFF0000FFFF;
            return value << 32 | value >> 32;
        }

        private static (ulong, ulong) WeakHashLen32WithSeeds(ulong w, ulong x, ulong y, ulong z, ulong a, ulong b)
        {
            a += w;
            b = Rotate(b + a + z, 21);
            var c = a;
            a += x;
            a += y;
            b += Rotate(a, 44);
            return (a + z, b + c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (ulong, ulong) WeakHashLen32WithSeeds(byte* s, ulong a, ulong b)
        {
            return WeakHashLen32WithSeeds(Fetch64(s), Fetch64(s + 8), Fetch64(s + 16), Fetch64(s + 24), a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLen16(ulong u, ulong v)
        {
            return Hash128To64((u, v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Hash128To64((ulong, ulong) x)
        {
            const ulong kMul = 0x9ddfea08eb382d69;
            var a = (Uint128Low64(x) ^ Uint128High64(x)) * kMul;
            a ^= a >> 47;
            var b = (Uint128High64(x) ^ a) * kMul;
            b ^= b >> 47;
            b *= kMul;
            return b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Uint128Low64((ulong, ulong) x)
        {
            return x.Item1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Uint128High64((ulong, ulong) x)
        {
            return x.Item2;
        }
    }
}
