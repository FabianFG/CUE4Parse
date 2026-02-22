using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace CUE4Parse.UE4.FMod.Utils;

// https://android.googlesource.com/platform/external/jenkins-hash/+/75dbeadebd95869dd623a29b720678c5c5c55630/lookup3.c
public static class JenkinsHash
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Rot(uint x, int k) => (x << k) | (x >> (32 - k));

    public static ulong Hash64(string input, uint pc = 0, uint pb = 0)
    {
        byte[] data = Encoding.UTF8.GetBytes(input);
        HashLittle2(data, ref pc, ref pb);
        return ((ulong) pb << 32) | pc;
    }

    // FMOD::hashlittle2, used for search in SoundTable for example
    public static void HashLittle2(ReadOnlySpan<byte> key, ref uint pc, ref uint pb)
    {
        uint length = (uint) key.Length;
        uint a, b, c;

        a = b = c = 0xdeadbeef + length + pc;
        c += pb;

        int offset = 0;
        int len = (int) length;

        while (len > 12)
        {
            a += BitConverter.ToUInt32(key.Slice(offset, 4));
            b += BitConverter.ToUInt32(key.Slice(offset + 4, 4));
            c += BitConverter.ToUInt32(key.Slice(offset + 8, 4));

            a -= c;
            a ^= Rot(c, 4);
            c += b;
            b -= a;
            b ^= Rot(a, 6);
            a += c;
            c -= b;
            c ^= Rot(b, 8);
            b += a;
            a -= c;
            a ^= Rot(c, 16);
            c += b;
            b -= a;
            b ^= Rot(a, 19);
            a += c;
            c -= b;
            c ^= Rot(b, 4);
            b += a;

            offset += 12;
            len -= 12;
        }

        switch (len)
        {
            case 12:
                c += (uint) key[offset + 11] << 24;
                goto case 11;
            case 11:
                c += (uint) key[offset + 10] << 16;
                goto case 10;
            case 10:
                c += (uint) key[offset + 9] << 8;
                goto case 9;
            case 9:
                c += key[offset + 8];
                goto case 8;
            case 8:
                b += (uint) key[offset + 7] << 24;
                goto case 7;
            case 7:
                b += (uint) key[offset + 6] << 16;
                goto case 6;
            case 6:
                b += (uint) key[offset + 5] << 8;
                goto case 5;
            case 5:
                b += key[offset + 4];
                goto case 4;
            case 4:
                a += (uint) key[offset + 3] << 24;
                goto case 3;
            case 3:
                a += (uint) key[offset + 2] << 16;
                goto case 2;
            case 2:
                a += (uint) key[offset + 1] << 8;
                goto case 1;
            case 1:
                a += key[offset];
                break;
            case 0:
                pc = c;
                pb = b;
                return;
        }

        c ^= b;
        c -= Rot(b, 14);
        a ^= c;
        a -= Rot(c, 11);
        b ^= a;
        b -= Rot(a, 25);
        c ^= b;
        c -= Rot(b, 16);
        a ^= c;
        a -= Rot(c, 4);
        b ^= a;
        b -= Rot(a, 14);
        c ^= b;
        c -= Rot(b, 24);

        pc = c;
        pb = b;
    }
}
