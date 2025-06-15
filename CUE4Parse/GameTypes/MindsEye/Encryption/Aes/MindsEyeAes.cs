using System;
using CUE4Parse.UE4.VirtualFileSystem;
using static CUE4Parse.GameTypes.SD.Encryption.Aes.SpectreDivideAes;

namespace CUE4Parse.GameTypes.MindsEye.Encryption.Aes;

public static class MindsEyeAes
{
    private const int AES_BLOCKBYTES = 16;
    private static readonly byte[] LookupTable = GenerateLookupTable();

    // 0x9293D1FF61DDF22F280FC7C20CF274396ADBBDAA4CD3B07BC4676154D2A1E06A
    public static uint[] rk =
    [
        0xac6cfc1b, 0x85236f9d, 0xf69d3e9e, 0x1b45e170,
        0x8e87e48a, 0xcc74bc2f, 0x9d960988, 0x5a1fd6b9,
        0x71c9163d, 0xfd23a10c, 0x71b2762a, 0x6b2a5114,
        0x4cc2f299, 0x42f358a5, 0x51e2b5a7, 0xc789df31,
        0xd312e550, 0x8ceab731, 0x8c91d726, 0x1a98273e,
        0x86dfc20a, 0x0e31aa3c, 0x1311ed02, 0x966b6a96,
        0xd4a17d5d, 0x5ff85261, 0x007b6017, 0x9609f018,
        0x046e9e9d, 0x88ee6836, 0x1d20473e, 0x857a8794,
        0xbc6cc584, 0x8b592f3c, 0x5f833276, 0x9672900f,
        0x2d4b931f, 0x8c80f6ab, 0x95ce2f08, 0x985ac0aa,
        0x2c2e6c15, 0x3735eab8, 0xd4da1d4a, 0xc9f1a279,
        0x03a2c789, 0xa1cb65b4, 0x194ed9a3, 0x0d94efa2,
        0x3489996e, 0x1b1b86ad, 0xe3eff7f2, 0x1d2bbf33,
        0xbae19c61, 0xa269a23d, 0xb885bc17, 0x14da3601,
        0xffd19392, 0x2ff2dd61, 0xc2c70f28, 0x3974f20c
    ];

    private static void rijndaelDecrypt(Span<uint> rk, Span<byte> input)
    {
        uint rbx_13 = (uint)((input[15] << 8 ^ input[14]) << 8 ^ input[13]) << 8 ^ input[12] ^ rk[3];
        uint r8_32 = (uint)((input[11] << 8 ^ input[10]) << 8 ^ input[9]) << 8 ^ input[8] ^ rk[2];
        uint r9_30 = (uint)((input[7] << 8 ^ input[6]) << 8 ^ input[5]) << 8 ^ input[4] ^ rk[1];
        uint rdx_41 = (uint)((input[3] << 8 ^ input[2]) << 8 ^ input[1]) << 8 ^ input[0] ^ rk[0];

        uint rdi_16 = Td2[(byte)(r9_30 >> 8)] ^ Td1[(byte)(r8_32 >> 16)] ^ Td0[rbx_13 >> 24] ^ Td3[(byte)rdx_41] ^ rk[7];
        uint r11_16 = Td2[(byte)(rdx_41 >> 8)] ^ Td1[(byte)(r9_30 >> 16)] ^ Td0[r8_32 >> 24] ^ Td3[(byte)rbx_13] ^ rk[6];
        uint r10_13 = Td1[(byte)(rdx_41 >> 16)] ^ Td2[(byte)(rbx_13 >> 8)] ^ Td0[r9_30 >> 24] ^ Td3[(byte)r8_32] ^ rk[5];
        uint r8_38 = Td2[(byte)(r8_32 >> 8)] ^ Td1[(byte)(rbx_13 >> 16)] ^ Td0[rdx_41 >> 24] ^ Td3[(byte)r9_30] ^ rk[4];

        uint rsi_19 = Td2[(byte)(r10_13 >> 8)] ^ Td1[(byte)(r11_16 >> 16)] ^ Td0[rdi_16 >> 24] ^ Td3[(byte)r8_38] ^ rk[11];
        uint rbx_19 = Td2[(byte)(r8_38 >> 8)] ^ Td1[(byte)(r10_13 >> 16)] ^ Td0[r11_16 >> 24] ^ Td3[(byte)rdi_16] ^ rk[10];
        uint r9_35 = Td1[(byte)(r8_38 >> 16)] ^ Td2[(byte)(rdi_16 >> 8)] ^ Td0[r10_13 >> 24] ^ Td3[(byte)r11_16] ^ rk[9];
        uint rdx_47 = Td2[(byte)(r11_16 >> 8)] ^ Td1[(byte)(rdi_16 >> 16)] ^ Td0[r8_38 >> 24] ^ Td3[(byte)r10_13] ^ rk[8];

        uint rdi_22 = Td2[(byte)(r9_35 >> 8)] ^ Td1[(byte)(rbx_19 >> 16)] ^ Td0[rsi_19 >> 24] ^ Td3[(byte)rdx_47] ^ rk[15];
        uint r11_22 = Td2[(byte)(rdx_47 >> 8)] ^ Td1[(byte)(r9_35 >> 16)] ^ Td0[rbx_19 >> 24] ^ Td3[(byte)rsi_19] ^ rk[14];
        uint r10_18 = Td1[(byte)(rdx_47 >> 16)] ^ Td2[(byte)(rsi_19 >> 8)] ^ Td0[r9_35 >> 24] ^ Td3[(byte)rbx_19] ^ rk[13];
        uint r8_44 = Td2[(byte)(rbx_19 >> 8)] ^ Td1[(byte)(rsi_19 >> 16)] ^ Td0[rdx_47 >> 24] ^ Td3[(byte)r9_35] ^ rk[12];

        uint rsi_25 = Td2[(byte)(r10_18 >> 8)] ^ Td1[(byte)(r11_22 >> 16)] ^ Td0[rdi_22 >> 24] ^ Td3[(byte)r8_44] ^ rk[19];
        uint rbx_25 = Td2[(byte)(r8_44 >> 8)] ^ Td1[(byte)(r10_18 >> 16)] ^ Td0[r11_22 >> 24] ^ Td3[(byte)rdi_22] ^ rk[18];
        uint r9_40 = Td1[(byte)(r8_44 >> 16)] ^ Td2[(byte)(rdi_22 >> 8)] ^ Td0[r10_18 >> 24] ^ Td3[(byte)r11_22] ^ rk[17];
        uint rdx_53 = Td2[(byte)(r11_22 >> 8)] ^ Td1[(byte)(rdi_22 >> 16)] ^ Td0[r8_44 >> 24] ^ Td3[(byte)r10_18] ^ rk[16];

        uint rdi_28 = Td2[(byte)(r9_40 >> 8)] ^ Td1[(byte)(rbx_25 >> 16)] ^ Td0[rsi_25 >> 24] ^ Td3[(byte)rdx_53] ^ rk[23];
        uint r11_28 = Td2[(byte)(rdx_53 >> 8)] ^ Td1[(byte)(r9_40 >> 16)] ^ Td0[rbx_25 >> 24] ^ Td3[(byte)rsi_25] ^ rk[22];
        uint r10_23 = Td1[(byte)(rdx_53 >> 16)] ^ Td2[(byte)(rsi_25 >> 8)] ^ Td0[r9_40 >> 24] ^ Td3[(byte)rbx_25] ^ rk[21];
        uint r8_50 = Td2[(byte)(rbx_25 >> 8)] ^ Td1[(byte)(rsi_25 >> 16)] ^ Td0[rdx_53 >> 24] ^ Td3[(byte)r9_40] ^ rk[20];

        uint rsi_31 = Td2[(byte)(r10_23 >> 8)] ^ Td1[(byte)(r11_28 >> 16)] ^ Td0[rdi_28 >> 24] ^ Td3[(byte)r8_50] ^ rk[27];
        uint rbx_31 = Td2[(byte)(r8_50 >> 8)] ^ Td1[(byte)(r10_23 >> 16)] ^ Td0[r11_28 >> 24] ^ Td3[(byte)rdi_28] ^ rk[26];
        uint r9_45 = Td1[(byte)(r8_50 >> 16)] ^ Td2[(byte)(rdi_28 >> 8)] ^ Td0[r10_23 >> 24] ^ Td3[(byte)r11_28] ^ rk[25];
        uint rdx_59 = Td2[(byte)(r11_28 >> 8)] ^ Td1[(byte)(rdi_28 >> 16)] ^ Td0[r8_50 >> 24] ^ Td3[(byte)r10_23] ^ rk[24];

        uint r14_15 = Td2[(byte)(r9_45 >> 8)] ^ Td1[(byte)(rbx_31 >> 16)] ^ Td0[rsi_31 >> 24] ^ Td3[(byte)rdx_59] ^ rk[31];
        uint r10_28 = Td2[(byte)(rdx_59 >> 8)] ^ Td1[(byte)(r9_45 >> 16)] ^ Td0[rbx_31 >> 24] ^ Td3[(byte)rsi_31] ^ rk[30];
        uint r11_34 = Td1[(byte)(rdx_59 >> 16)] ^ Td2[(byte)(rsi_31 >> 8)] ^ Td0[r9_45 >> 24] ^ Td3[(byte)rbx_31] ^ rk[29];
        uint r8_56 = Td2[(byte)(rbx_31 >> 8)] ^ Td1[(byte)(rsi_31 >> 16)] ^ Td0[rdx_59 >> 24] ^ Td3[(byte)r9_45] ^ rk[28];

        uint rsi_37 = Td2[(byte)(r11_34 >> 8)] ^ Td1[(byte)(r10_28 >> 16)] ^ Td0[r14_15 >> 24] ^ Td3[(byte)r8_56] ^ rk[35];
        uint rdi_34 = Td2[(byte)(r8_56 >> 8)] ^ Td1[(byte)(r11_34 >> 16)] ^ Td0[r10_28 >> 24] ^ Td3[(byte)r14_15] ^ rk[34];
        uint rbx_37 = Td1[(byte)(r8_56 >> 16)] ^ Td2[(byte)(r14_15 >> 8)] ^ Td0[r11_34 >> 24] ^ Td3[(byte)r10_28] ^ rk[33];
        uint r10_34 = Td2[(byte)(r10_28 >> 8)] ^ Td1[(byte)(r14_15 >> 16)] ^ Td0[r8_56 >> 24] ^ Td3[(byte)r11_34] ^ rk[32];

        uint r11_39 = Td2[(byte)(rdi_34 >> 8)] ^ Td1[(byte)(rsi_37 >> 16)] ^ Td0[r10_34 >> 24] ^ Td3[(byte)rbx_37] ^ rk[36];
        uint r9_50 = Td1[(byte)(r10_34 >> 16)] ^ Td2[(byte)(rsi_37 >> 8)] ^ Td0[rbx_37 >> 24] ^ Td3[(byte)rdi_34] ^ rk[37];
        uint r10_35 = Td2[(byte)(r10_34 >> 8)] ^ Td1[(byte)(rbx_37 >> 16)] ^ Td0[rdi_34 >> 24] ^ Td3[(byte)rsi_37] ^ rk[38];
        uint rdx_69 = Td2[(byte)(rbx_37 >> 8)] ^ Td1[(byte)(rdi_34 >> 16)] ^ Td0[rsi_37 >> 24] ^ Td3[(byte)r10_34] ^ rk[39];

        uint r14_21 = Td2[(byte)(r9_50 >> 8)] ^ Td1[(byte)(r10_35 >> 16)] ^ Td0[rdx_69 >> 24] ^ Td3[(byte)r11_39] ^ rk[43];
        uint rsi_43 = Td2[(byte)(r11_39 >> 8)] ^ Td1[(byte)(r9_50 >> 16)] ^ Td0[r10_35 >> 24] ^ Td3[(byte)rdx_69] ^ rk[42];
        uint rdi_40 = Td1[(byte)(r11_39 >> 16)] ^ Td2[(byte)(rdx_69 >> 8)] ^ Td0[r9_50 >> 24] ^ Td3[(byte)r10_35] ^ rk[41];
        uint rbx_43 = Td2[(byte)(r10_35 >> 8)] ^ Td1[(byte)(rdx_69 >> 16)] ^ Td0[r11_39 >> 24] ^ Td3[(byte)r9_50] ^ rk[40];

        uint r11_44 = Td2[(byte)(rsi_43 >> 8)] ^ Td1[(byte)(r14_21 >> 16)] ^ Td0[rbx_43 >> 24] ^ Td3[(byte)rdi_40] ^ rk[44];
        uint r9_55 = Td1[(byte)(rbx_43 >> 16)] ^ Td2[(byte)(r14_21 >> 8)] ^ Td0[rdi_40 >> 24] ^ Td3[(byte)rsi_43] ^ rk[45];
        uint r10_37 = Td2[(byte)(rbx_43 >> 8)] ^ Td1[(byte)(rdi_40 >> 16)] ^ Td0[rsi_43 >> 24] ^ Td3[(byte)r14_21] ^ rk[46];
        uint rdx_79 = Td2[(byte)(rdi_40 >> 8)] ^ Td1[(byte)(rsi_43 >> 16)] ^ Td0[r14_21 >> 24] ^ Td3[(byte)rbx_43] ^ rk[47];

        uint rsi_49 = Td2[(byte)(r9_55 >> 8)] ^ Td1[(byte)(r10_37 >> 16)] ^ Td0[rdx_79 >> 24] ^ Td3[(byte)r11_44] ^ rk[51];
        uint rdi_46 = Td2[(byte)(r11_44 >> 8)] ^ Td1[(byte)(r9_55 >> 16)] ^ Td0[r10_37 >> 24] ^ Td3[(byte)rdx_79] ^ rk[50];
        uint rbx_48 = Td1[(byte)(r11_44 >> 16)] ^ Td2[(byte)(rdx_79 >> 8)] ^ Td0[r9_55 >> 24] ^ Td3[(byte)r10_37] ^ rk[49];
        uint r11_49 = Td2[(byte)(r10_37 >> 8)] ^ Td1[(byte)(rdx_79 >> 16)] ^ Td0[r11_44 >> 24] ^ Td3[(byte)r9_55] ^ rk[48];

        uint r10_43 = Td2[(byte)(rdi_46 >> 8)] ^ Td1[(byte)(rsi_49 >> 16)] ^ Td0[r11_49 >> 24] ^ Td3[(byte)rbx_48] ^ rk[52];
        uint r9_60 = Td1[(byte)(r11_49 >> 16)] ^ Td2[(byte)(rsi_49 >> 8)] ^ Td0[rbx_48 >> 24] ^ Td3[(byte)rdi_46] ^ rk[53];
        uint r11_50 = Td2[(byte)(r11_49 >> 8)] ^ Td1[(byte)(rbx_48 >> 16)] ^ Td0[rdi_46 >> 24] ^ Td3[(byte)rsi_49] ^ rk[54];
        uint rdx_88 = Td2[(byte)(rbx_48 >> 8)] ^ Td1[(byte)(rdi_46 >> 16)] ^ Td0[rsi_49 >> 24] ^ Td3[(byte)r11_49] ^ rk[55];

        uint rbx_50 = rdx_88;
        uint rdx_94 = (Td4[(byte) (rdx_88 >> 0x10)] & 0xff0000) ^ (Td4[(byte)(r11_50 >> 8)] & 0xff00) ^ (Td4[r10_43 >> 24] & 0xff000000) ^ (byte)(Td4[(byte)r9_60]) ^ rk[56];
        input[3] = (byte) (rdx_94 >> 0x18);
        input[2] = (byte) (rdx_94 >> 0x10);
        input[1] = (byte) (rdx_94 >> 8);
        input[0] = (byte) rdx_94;
        uint rdx_100 = (Td4[(byte) (r10_43 >> 0x10)] & 0xff0000) ^ (Td4[(byte)(rbx_50 >> 8)] & 0xff00) ^ (Td4[r9_60 >> 24] & 0xff000000) ^ (byte)(Td4[(byte)r11_50]) ^ rk[57];
        input[7] = (byte)(rdx_100 >> 0x18);
        input[6] = (byte)(rdx_100 >> 0x10);
        input[5] = (byte)(rdx_100 >> 8);
        input[4] = (byte)rdx_100;
        uint rdx_106 = (Td4[(byte) (r9_60 >> 0x10)] & 0xff0000) ^ (Td4[(byte)(r10_43 >> 8)] & 0xff00) ^ (Td4[r11_50 >> 24] & 0xff000000) ^ (byte)(Td4[(byte)rbx_50]) ^ rk[58];
        input[11] = (byte)(rdx_106 >> 0x18);
        input[10] = (byte)(rdx_106 >> 0x10);
        input[9] = (byte)(rdx_106 >> 8);
        input[8] = (byte)rdx_106;
        uint rdx_112 = (Td4[(byte) (r11_50 >> 0x10)] & 0xff0000) ^ (Td4[(byte)(r9_60 >> 8)] & 0xff00) ^ (Td4[rbx_50 >> 24] & 0xff000000) ^ (byte)(Td4[(byte)r10_43]) ^ rk[59];
        input[15] = (byte)(rdx_112 >> 0x18);
        input[14] = (byte)(rdx_112 >> 0x10);
        input[13] = (byte)(rdx_112 >> 8);
        input[12] = (byte)rdx_112;
    }

    public static byte[] MindsEyeDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var plaintext = bytes[beginOffset..(beginOffset+count)];
        for (var offset = 0; offset < count; offset += AES_BLOCKBYTES)
        {
            rijndaelDecrypt(rk, plaintext.AsSpan()[offset..]);
        }

        Span<byte> span = plaintext.AsSpan();
        if (isIndex)
        {
            long r14 = 0x23212002;
            for (var i = 0; i < count; i++)
            {
                byte rax = span[i];
                byte r10 = (byte)(r14 & 0xFF | rax);
                byte rcx = (byte)(~rax);
                byte r11 = (byte)((r14 & 0xFF) & rcx);
                byte rbx = (byte)(r10 + (r14 & 0xFF));
                byte r9 = (byte)((r14 & 0xFF) ^ rax);
                byte rsi = (byte)((r14 & 0xFF) & rax);
                byte r8 = (byte)((r14 & 0xFF) | rcx);
                byte rdi = (byte)(r11 * 2 + rsi);
                byte r8_calc = (byte)(
                    ((((~r10 + 1 + rsi) << 2) + r9 * 3 - r8 + r11) * 2 +
                     ((rdi - (r14 & 0xFF)) * 2 - r9) * 9) * (~r8) +
                    (((rbx - (r11 + rsi) * 2) * 2 - r11 * 3 + r9) * 3 - 1) * r9 +
                    (rdi * 0x12 - r10 * 0x18 + 4) * r11 +
                    (r10 * 0x0C - r11 * 0x12 - 2) * (byte)(r14 & 0xFF)
                );

                long r8_zx = (r8_calc + ((rdi - rbx) * 0x0C + 2) * rsi);
                span[i] = (byte)r8_zx;
                long rdx = ~(int)r8_zx;
                r14 = (r14 & rdx) + (~(rdx | r14)) * 3 + ~(r8_zx ^ r14) + (r8_zx | r14) + (r8_zx & r14) - r8_zx * 2 + 1;
            }
        }
        else
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = LookupTable[span[i]];
            }
        }
        return plaintext;
    }

    private static byte[] GenerateLookupTable()
    {
        var result = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            byte dh = (byte)i;
            byte al = dh;
            byte cl = dh;

            al &= 0xCB;
            cl &= 0x34;

            int ebx = al * 0xFD;
            int eax = cl * 0xFD;
            int var_fc = ebx;

            byte dl = (byte)(ebx & 0xFF);
            byte bl = dh;

            cl = (byte)~cl;
            dl = (byte)~dl;
            bl |= 0x34;

            bl += cl;
            dh |= 0xCB;

            int ecx = eax;
            cl = (byte)(ecx & 0xFF);
            bl += bl;

            al = (byte)(ecx & 0xFF);
            al |= dl;
            al = (byte)~al;
            bl += al;

            al = (byte)(ecx & 0xFF);
            al &= dl;
            bl += al;

            al = (byte)(ecx & 0xFF);
            al &= (byte)var_fc;
            cl ^= (byte)var_fc;
            bl += al;

            al = dh;
            cl = (byte)~cl;
            dh += dh;
            cl -= 0x59;
            al += dh;
            bl += al;

            bl += bl;
            bl += cl;

            result[i] = bl;
        }
        return result;
    }
}
