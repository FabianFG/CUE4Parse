using System;
using CUE4Parse.UE4.VirtualFileSystem;
using static CUE4Parse.GameTypes.NetEase.MAR.Encryption.Aes.MarvelAes;

namespace CUE4Parse.GameTypes.THPS.Encryption.Aes;

/// <summary>
/// Reversed by Spiritovod
/// </summary>
public static class THPS12Aes
{
    public static byte[] THPS12Decrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");

        var ciphertext = bytes[beginOffset..];
        var plaintext = new byte[count];
        for (var offset = 0; offset < count; offset += 16)
        {
            THPS12rijndaelDecrypt(RoundKeys, 14, ciphertext.AsSpan()[offset..], plaintext.AsSpan()[offset..]);
        }

        return plaintext;
    }

    private static readonly uint[] RoundKeys =
    [
        0x47269fd9, 0x25e25346, 0x338ad3e4, 0x6c53a789, 0x5407ba92, 0xc46782fd, 0xabb54f69, 0x456919ba,
        0x3e13aaab, 0xa7bff815, 0x0e2c205e, 0xef75e1e4, 0xc96d860d, 0x9060386f, 0x6fd2cd94, 0xeedc56d3,
        0xed789294, 0x99ac52be, 0xa993d84b, 0xe159c1ba, 0x96b60cc5, 0x590dbe62, 0xffb2f5fb, 0x810e9b47,
        0xd23a67c7, 0x74d4c02a, 0x303f8af5, 0x48ca19f1, 0x011d7ae9, 0xcfbbb2a7, 0xa6bf4b99, 0x7ebc6ebc,
        0xdf516cc2, 0xa6eea7ed, 0x44eb4adf, 0x78f59304, 0x492df62b, 0xcea6c84e, 0x6904f93e, 0xd8032525,
        0x669cff94, 0x79bfcb2f, 0xe205ed32, 0x3c1ed9db, 0xcdc5fb27, 0x878b3e65, 0xa7a23170, 0xb107dc1b,
        0x7ccf0607, 0x1f2334bb, 0x9bba261d, 0xde1b34e9, 0x1ed7860a, 0x4a4ec542, 0x20290f15, 0x16a5ed6b,
        0xeabd3620, 0x674a3418, 0x173dc84b, 0x94dec78f
    ];

    private static void THPS12rijndaelDecrypt(Span<uint> rk, int nrounds, Span<byte> ciphertext, Span<byte> plaintext)
    {
        /*
         * map byte array block to cipher state
         * and add initial round key:
         */
        var s0 = THPSGETU32(ciphertext[..]) ^ rk[0];
        var s1 = THPSGETU32(ciphertext[4..]) ^ rk[1];
        var s2 = THPSGETU32(ciphertext[8..]) ^ rk[2];
        var s3 = THPSGETU32(ciphertext[12..]) ^ rk[3];

        /* round 1: */
        var t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[4];
        var t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[5];
        var t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[6];
        var t3 = Td0[s3 >> 24] ^ Td1[(s2 >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[7];
        /* round 2: */
        s0 = Td0[t0 >> 24] ^ Td1[(t3 >> 16) & 0xff] ^ Td2[(t2 >> 8) & 0xff] ^ Td3[t1 & 0xff] ^ rk[8];
        s1 = Td0[t1 >> 24] ^ Td1[(t0 >> 16) & 0xff] ^ Td2[(t3 >> 8) & 0xff] ^ Td3[t2 & 0xff] ^ rk[9];
        s2 = Td0[t2 >> 24] ^ Td1[(t1 >> 16) & 0xff] ^ Td2[(t0 >> 8) & 0xff] ^ Td3[t3 & 0xff] ^ rk[10];
        s3 = Td0[t3 >> 24] ^ Td1[(t2 >> 16) & 0xff] ^ Td2[(t1 >> 8) & 0xff] ^ Td3[t0 & 0xff] ^ rk[11];
        /* round 3: */
        t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[12];
        t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[13];
        t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[14];
        t3 = Td0[s3 >> 24] ^ Td1[(s2 >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[15];
        /* round 4: */
        s0 = Td0[t0 >> 24] ^ Td1[(t3 >> 16) & 0xff] ^ Td2[(t2 >> 8) & 0xff] ^ Td3[t1 & 0xff] ^ rk[16];
        s1 = Td0[t1 >> 24] ^ Td1[(t0 >> 16) & 0xff] ^ Td2[(t3 >> 8) & 0xff] ^ Td3[t2 & 0xff] ^ rk[17];
        s2 = Td0[t2 >> 24] ^ Td1[(t1 >> 16) & 0xff] ^ Td2[(t0 >> 8) & 0xff] ^ Td3[t3 & 0xff] ^ rk[18];
        s3 = Td0[t3 >> 24] ^ Td1[(t2 >> 16) & 0xff] ^ Td2[(t1 >> 8) & 0xff] ^ Td3[t0 & 0xff] ^ rk[19];
        var tk = Replace(s2, t2);
        /* round 5: */
        t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[20];
        t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[21];
        t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[22];
        t3 = Td0[s3 >> 24] ^ Td1[(tk >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[23];
        tk = Replace(t2, tk);
        /* round 6: */
        s0 = Td0[t0 >> 24] ^ Td1[(t3 >> 16) & 0xff] ^ Td2[(t2 >> 8) & 0xff] ^ Td3[t1 & 0xff] ^ rk[24];
        s1 = Td0[t1 >> 24] ^ Td1[(t0 >> 16) & 0xff] ^ Td2[(t3 >> 8) & 0xff] ^ Td3[t2 & 0xff] ^ rk[25];
        s2 = Td0[t2 >> 24] ^ Td1[(t1 >> 16) & 0xff] ^ Td2[(t0 >> 8) & 0xff] ^ Td3[t3 & 0xff] ^ rk[26];
        s3 = Td0[t3 >> 24] ^ Td1[(tk >> 16) & 0xff] ^ Td2[(t1 >> 8) & 0xff] ^ Td3[t0 & 0xff] ^ rk[27];
        tk = Replace(s2, tk);
        /* round 7: */
        t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[28];
        t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[29];
        t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[30];
        t3 = Td0[s3 >> 24] ^ Td1[(tk >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[31];
        /* round 8: */
        s0 = Td0[t0 >> 24] ^ Td1[(t3 >> 16) & 0xff] ^ Td2[(t2 >> 8) & 0xff] ^ Td3[t1 & 0xff] ^ rk[32];
        s1 = Td0[t1 >> 24] ^ Td1[(t0 >> 16) & 0xff] ^ Td2[(t3 >> 8) & 0xff] ^ Td3[t2 & 0xff] ^ rk[33];
        s2 = Td0[t2 >> 24] ^ Td1[(t1 >> 16) & 0xff] ^ Td2[(t0 >> 8) & 0xff] ^ Td3[t3 & 0xff] ^ rk[34];
        s3 = Td0[t3 >> 24] ^ Td1[(t2 >> 16) & 0xff] ^ Td2[(t1 >> 8) & 0xff] ^ Td3[t0 & 0xff] ^ rk[35];
        /* round 9: */
        t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[36];
        t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[37];
        t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[38];
        t3 = Td0[s3 >> 24] ^ Td1[(s2 >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[39];
        if (nrounds > 10)
        {
            /* round 10: */
            s0 = Td0[t0 >> 24] ^ Td1[(t3 >> 16) & 0xff] ^ Td2[(t2 >> 8) & 0xff] ^ Td3[t1 & 0xff] ^ rk[40];
            s1 = Td0[t1 >> 24] ^ Td1[(t0 >> 16) & 0xff] ^ Td2[(t3 >> 8) & 0xff] ^ Td3[t2 & 0xff] ^ rk[41];
            s2 = Td0[t2 >> 24] ^ Td1[(t1 >> 16) & 0xff] ^ Td2[(t0 >> 8) & 0xff] ^ Td3[t3 & 0xff] ^ rk[42];
            s3 = Td0[t3 >> 24] ^ Td1[(t2 >> 16) & 0xff] ^ Td2[(t1 >> 8) & 0xff] ^ Td3[t0 & 0xff] ^ rk[43];
            /* round 11: */
            t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[44];
            t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[45];
            t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[46];
            t3 = Td0[s3 >> 24] ^ Td1[(s2 >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[47];
            if (nrounds > 12)
            {
                /* round 12: */
                s0 = Td0[t0 >> 24] ^ Td1[(t3 >> 16) & 0xff] ^ Td2[(t2 >> 8) & 0xff] ^ Td3[t1 & 0xff] ^ rk[48];
                s1 = Td0[t1 >> 24] ^ Td1[(t0 >> 16) & 0xff] ^ Td2[(t3 >> 8) & 0xff] ^ Td3[t2 & 0xff] ^ rk[49];
                s2 = Td0[t2 >> 24] ^ Td1[(t1 >> 16) & 0xff] ^ Td2[(t0 >> 8) & 0xff] ^ Td3[t3 & 0xff] ^ rk[50];
                s3 = Td0[t3 >> 24] ^ Td1[(t2 >> 16) & 0xff] ^ Td2[(t1 >> 8) & 0xff] ^ Td3[t0 & 0xff] ^ rk[51];
                /* round 13: */
                t0 = Td0[s0 >> 24] ^ Td1[(s3 >> 16) & 0xff] ^ Td2[(s2 >> 8) & 0xff] ^ Td3[s1 & 0xff] ^ rk[52];
                t1 = Td0[s1 >> 24] ^ Td1[(s0 >> 16) & 0xff] ^ Td2[(s3 >> 8) & 0xff] ^ Td3[s2 & 0xff] ^ rk[53];
                t2 = Td0[s2 >> 24] ^ Td1[(s1 >> 16) & 0xff] ^ Td2[(s0 >> 8) & 0xff] ^ Td3[s3 & 0xff] ^ rk[54];
                t3 = Td0[s3 >> 24] ^ Td1[(s2 >> 16) & 0xff] ^ Td2[(s1 >> 8) & 0xff] ^ Td3[s0 & 0xff] ^ rk[55];
            }
        }

        rk = rk[(nrounds << 2)..];
        /*
         * apply last round and
         * map cipher state to byte array block:
         */
        s0 =
            (Td4[(t0 >> 24) & 0xff] & 0xff000000) ^
            (Td4[(t3 >> 16) & 0xff] & 0x00ff0000) ^
            (Td4[(t2 >> 8) & 0xff] & 0x0000ff00) ^
            (Td4[(t1 >> 0) & 0xff] & 0x000000ff) ^
            rk[0];
        THPSPUTU32(plaintext[..], s0);

        s1 =
            (Td4[(t1 >> 24) & 0xff] & 0xff000000) ^
            (Td4[(t0 >> 16) & 0xff] & 0x00ff0000) ^
            (Td4[(t3 >> 8) & 0xff] & 0x0000ff00) ^
            (Td4[(t2 >> 0) & 0xff] & 0x000000ff) ^
            rk[1];
        THPSPUTU32(plaintext[4..], s1);
        plaintext[7] = (byte)(rk[1] ^ Td4[(byte)t2]);

        s2 =
            (Td4[(t2 >> 24) & 0xff] & 0xff000000) ^
            (Td4[(t1 >> 16) & 0xff] & 0x00ff0000) ^
            (Td4[(t0 >> 8) & 0xff] & 0x0000ff00) ^
            (Td4[(t3 >> 0) & 0xff] & 0x000000ff) ^
            rk[2];
        THPSPUTU32(plaintext[8..], s2);
        plaintext[11] = (byte)(rk[2] ^ Td4[(byte)t3]);

        s3 =
            (Td4[(t3 >> 24) & 0xff] & 0xff000000) ^
            (Td4[(t2 >> 16) & 0xff] & 0x00ff0000) ^
            (Td4[(t1 >> 8) & 0xff] & 0x0000ff00) ^
            (Td4[(t0 >> 0) & 0xff] & 0x000000ff) ^
            rk[3];
        THPSPUTU32(plaintext[12..], s3);
        return;

        uint THPSGETU32(Span<byte> plaintext) => (uint) (plaintext[0] << 24 | plaintext[1] << 16 | plaintext[2] << 8 | plaintext[3]);

        void THPSPUTU32(Span<byte> plaintext, uint st)
        {
            plaintext[0] = (byte) (st >> 24);
            plaintext[1] = (byte) (st >> 16);
            plaintext[2] = (byte) (st >> 8);
            plaintext[3] = (byte) st;
        }

        uint Replace(uint input, uint output) => (output & 0xFF000000) | (input & 0x00FF0000) | (output & 0x0000FF00) | (output & 0x000000FF);
    }
}
