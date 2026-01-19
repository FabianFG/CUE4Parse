using System;
using CUE4Parse.UE4.VirtualFileSystem;
using static CUE4Parse.GameTypes.NetEase.MAR.Encryption.Aes.MarvelAes;

namespace CUE4Parse.GameTypes.NMZ.Encryption.Aes;

/// <summary>
/// Reversed by Spiritovod
/// </summary>
public class AssaultFireFutureAes
{
    private static void rijndaelDecrypt(Span<uint> rk, Span<byte> a3, Span<byte> a4)
    {
        uint ecx_7 = (uint)((((((a3[0] << 8) ^ a3[1]) << 8) ^ a3[2]) << 8) ^ a3[3]) ^ rk[0];
        uint ecx_15 = (uint) ((((((a3[4] << 8) ^ a3[5]) << 8) ^ a3[6]) << 8) ^ a3[7]) ^ rk[1];
        uint ecx_16 = rk[2];
        uint edx = (ushort) ecx_16;
        uint ebx_4 = (uint)(((a3[8] << 8) ^ a3[9]) << 16) ^ ecx_16;
        uint ecx_24 = (uint)((((((a3[0x0C] << 8) ^ a3[0x0D]) << 8) ^ a3[0x0E]) << 8) ^ a3[0x0F]) ^ rk[3];
        uint ecx_32 = Td2[(a3[0x0A] ^ (edx >> 8)) & 0xFF] ^ Td1[(byte) (ecx_24 >> 16)] ^ Td0[(byte) (ecx_7 >> 24)] ^ Td3[(byte) ecx_15] ^ rk[4];
        uint eax_30 = Td3[(a3[0x0B] ^ (byte) edx) & 0xFF] ^ Td2[(byte) (ecx_24 >> 8)] ^ Td1[(byte) (ecx_7 >> 16)] ^ Td0[(byte) (ecx_15 >> 24)] ^ rk[5];
        uint ecx_43 = Td1[(byte) (ecx_15 >> 16)] ^ Td2[(byte) (ecx_7 >> 8)] ^ Td0[(byte) (ebx_4 >> 24)] ^ Td3[(byte) ecx_24] ^ rk[6];
        uint edx_6 = Td1[(byte) (ebx_4 >> 16)] ^ Td2[(byte) (ecx_15 >> 8)] ^ Td0[(byte) (ecx_24 >> 24)] ^ Td3[(byte) ecx_7] ^ rk[7];
        uint ecx_50 = Td1[(byte) (edx_6 >> 16)] ^ Td2[(byte) (ecx_43 >> 8)] ^ Td0[(byte) (ecx_32 >> 24)] ^ Td3[(byte) eax_30] ^ rk[8];
        uint ecx_56 = Td2[(byte) (edx_6 >> 8)] ^ Td1[(byte) (ecx_32 >> 16)] ^ Td0[(byte) (eax_30 >> 24)] ^ Td3[(byte) ecx_43] ^ rk[9];
        uint ecx_62 = Td1[(byte) (eax_30 >> 16)] ^ Td2[(byte) (ecx_32 >> 8)] ^ Td0[(byte) (ecx_43 >> 24)] ^ Td3[(byte) edx_6] ^ rk[10];
        uint ebx_12 = Td1[(byte) (ecx_43 >> 16)] ^ Td2[(byte) (eax_30 >> 8)] ^ Td0[(byte) (edx_6 >> 24)] ^ Td3[(byte) ecx_32] ^ rk[11];
        uint ecx_69 = Td1[(byte) (ebx_12 >> 16)] ^ Td2[(byte) (ecx_62 >> 8)] ^ Td0[(byte) (ecx_50 >> 24)] ^ Td3[(byte) ecx_56] ^ rk[12];
        uint eax_95 = Td2[(byte) (ebx_12 >> 8)] ^ Td1[(byte) (ecx_50 >> 16)] ^ Td0[(byte) (ecx_56 >> 24)] ^ Td3[(byte) ecx_62] ^ rk[13];
        uint ecx_78 = Td1[(byte) (ecx_56 >> 16)] ^ Td2[(byte) (ecx_50 >> 8)] ^ Td0[(byte) (ecx_62 >> 24)] ^ Td3[(byte) ebx_12] ^ rk[14];
        uint edx_14 = Td1[(byte) (ecx_62 >> 16)] ^ Td2[(byte) (ecx_56 >> 8)] ^ Td0[(byte) (ebx_12 >> 24)] ^ Td3[(byte) ecx_50] ^ rk[15];
        uint ecx_85 = Td1[(byte) (edx_14 >> 16)] ^ Td2[(byte) (ecx_78 >> 8)] ^ Td0[(byte) (ecx_69 >> 24)] ^ Td3[(byte) eax_95] ^ rk[16];
        uint eax_127 = Td2[(byte) (edx_14 >> 8)] ^ Td1[(byte) (ecx_69 >> 16)] ^ Td0[(byte) (eax_95 >> 24)] ^ Td3[(byte) ecx_78] ^ rk[17];
        uint ecx_94 = Td1[(byte) (eax_95 >> 16)] ^ Td2[(byte) (ecx_69 >> 8)] ^ Td0[(byte) (ecx_78 >> 24)] ^ Td3[(byte) edx_14] ^ rk[18];
        uint ebx_20 = Td1[(byte) (ecx_78 >> 16)] ^ Td2[(byte) (eax_95 >> 8)] ^ Td0[(byte) (edx_14 >> 24)] ^ Td3[(byte) ecx_69] ^ rk[19];
        uint ecx_101 = Td1[(byte) (ebx_20 >> 16)] ^ Td2[(byte) (ecx_94 >> 8)] ^ Td0[(byte) (ecx_85 >> 24)] ^ Td3[(byte) eax_127] ^ rk[20];
        uint eax_159 = Td2[(byte) (ebx_20 >> 8)] ^ Td1[(byte) (ecx_85 >> 16)] ^ Td0[(byte) (eax_127 >> 24)] ^ Td3[(byte) ecx_94] ^ rk[21];
        uint ecx_110 = Td1[(byte) (eax_127 >> 16)] ^ Td2[(byte) (ecx_85 >> 8)] ^ Td0[(byte) (ecx_94 >> 24)] ^ Td3[(byte) ebx_20] ^ rk[22];
        uint edx_22 = Td1[(byte) (ecx_94 >> 16)] ^ Td2[(byte) (eax_127 >> 8)] ^ Td0[(byte) (ebx_20 >> 24)] ^ Td3[(byte) ecx_85] ^ rk[23];
        uint ecx_117 = Td1[(byte) (edx_22 >> 16)] ^ Td2[(byte) (ecx_110 >> 8)] ^ Td0[(byte) (ecx_101 >> 24)] ^ Td3[(byte) eax_159] ^ rk[24];
        uint eax_191 = Td2[(byte) (edx_22 >> 8)] ^ Td1[(byte) (ecx_101 >> 16)] ^ Td0[(byte) (eax_159 >> 24)] ^ Td3[(byte) ecx_110] ^ rk[25];
        uint ecx_126 = Td1[(byte) (eax_159 >> 16)] ^ Td2[(byte) (ecx_101 >> 8)] ^ Td0[(byte) (ecx_110 >> 24)] ^ Td3[(byte) edx_22] ^ rk[26];
        uint ebx_28 = Td1[(byte) (ecx_110 >> 16)] ^ Td2[(byte) (eax_159 >> 8)] ^ Td0[(byte) (edx_22 >> 24)] ^ Td3[(byte) ecx_101] ^ rk[27];
        uint ecx_133 = Td1[(byte) (ebx_28 >> 16)] ^ Td2[(byte) (ecx_126 >> 8)] ^ Td0[(byte) (ecx_117 >> 24)] ^ Td3[(byte) eax_191] ^ rk[28];
        uint eax_223 = Td2[(byte) (ebx_28 >> 8)] ^ Td1[(byte) (ecx_117 >> 16)] ^ Td0[(byte) (eax_191 >> 24)] ^ Td3[(byte) ecx_126] ^ rk[29];
        uint ecx_142 = Td1[(byte) (eax_191 >> 16)] ^ Td2[(byte) (ecx_117 >> 8)] ^ Td0[(byte) (ecx_126 >> 24)] ^ Td3[(byte) ebx_28] ^ rk[30];
        uint edx_30 = Td1[(byte) (ecx_126 >> 16)] ^ Td2[(byte) (eax_191 >> 8)] ^ Td0[(byte) (ebx_28 >> 24)] ^ Td3[(byte) ecx_117] ^ rk[31];
        uint eax_246 = Td2[(byte) (edx_30 >> 8)] ^ Td1[(byte) (ecx_133 >> 16)] ^ Td0[(byte) (eax_223 >> 24)] ^ Td3[(byte) ecx_142] ^ rk[33];
        uint ecx_150 = Td1[(byte) (eax_223 >> 16)] ^ Td2[(byte) (ecx_133 >> 8)] ^ Td0[(byte) (ecx_142 >> 24)] ^ Td3[(byte) edx_30] ^ rk[34];
        uint ecx_156 = Td1[(byte) (edx_30 >> 16)] ^ Td2[(byte) (ecx_142 >> 8)] ^ Td0[(byte) (ecx_133 >> 24)] ^ Td3[(byte) eax_223] ^ rk[32];
        uint ebx_39 = Td1[(byte) (ecx_142 >> 16)] ^ Td2[(byte) (eax_223 >> 8)] ^ Td0[(byte) (edx_30 >> 24)] ^ Td3[(byte) ecx_133] ^ rk[35];
        uint ecx_163 = Td1[(byte) (ebx_39 >> 16)] ^ Td2[(byte) (ecx_150 >> 8)] ^ Td0[(byte) (ecx_156 >> 24)] ^ Td3[(byte) eax_246] ^ rk[36];
        uint ecx_169 = Td2[(byte) (ebx_39 >> 8)] ^ Td1[(byte) (ecx_156 >> 16)] ^ Td0[(byte) (eax_246 >> 24)] ^ Td3[(byte) ecx_150] ^ rk[37];
        uint eax_296 = Td2[(byte) (ecx_156 >> 8)] ^ Td1[(byte) (eax_246 >> 16)] ^ Td0[(byte) (ecx_150 >> 24)] ^ Td3[(byte) ebx_39] ^ rk[38];
        uint eax_306 = Td2[(byte) (eax_246 >> 8)] ^ Td1[(byte) (ecx_150 >> 16)] ^ Td0[(byte) (ebx_39 >> 24)] ^ Td3[(byte) ecx_156] ^ rk[39];
        uint eax_316 = Td2[(byte) (eax_306 >> 8)] ^ Td1[(byte) (ecx_163 >> 16)] ^ Td0[(byte) (ecx_169 >> 24)] ^ Td3[(byte) eax_296] ^ rk[41];
        uint eax_331 = Td1[(byte) (ecx_169 >> 16)] ^ Td2[(byte) (ecx_163 >> 8)] ^ Td0[(byte) (eax_296 >> 24)] ^ Td3[(byte) (eax_306)] ^ rk[42];
        uint ecx_192 = Td1[(byte) (eax_306 >> 16)] ^  Td2[(byte) (eax_296 >> 8)] ^ Td0[(byte) (ecx_163 >> 24)] ^ Td3[(byte) ecx_169] ^ rk[40];
        uint ebx_52 = Td1[(byte) (eax_296 >> 16)] ^ Td2[(byte) (ecx_169 >> 8)] ^ Td0[(byte) (eax_306 >> 24)] ^ Td3[(byte) ecx_163] ^ rk[43];
        uint eax_358 = Td2[(byte) (eax_331 >> 8)] ^ Td1[(byte) (ebx_52 >> 16)] ^ Td0[(byte) (ecx_192 >> 24)] ^ Td3[(byte) eax_316] ^ rk[44];
        uint ecx_200 = Td2[(byte) (ebx_52 >> 8)] ^ Td1[(byte) (ecx_192 >> 16)] ^ Td0[(byte) (eax_316 >> 24)] ^ Td3[(byte) eax_331] ^ rk[45];
        uint ebx_63 = Td2[(byte) (ecx_192 >> 8)] ^ Td1[(byte) (eax_316 >> 16)] ^ Td0[(byte) (eax_331 >> 24)] ^ Td3[(byte) ebx_52] ^ rk[46];
        uint ebx_68 = Td2[(byte) (eax_316 >> 8)] ^ Td3[(byte) ecx_192] ^ Td1[(byte) (eax_331 >> 16)] ^ Td0[(byte) (ebx_52 >> 24)] ^ rk[47];
        uint ecx_215 = Td1[(byte) (ebx_68 >> 16)] ^ Td2[(byte) (ebx_63 >> 8)] ^ Td0[(byte) (eax_358 >> 24)] ^ Td3[(byte) ecx_200] ^ rk[48];
        uint ecx_221 = Td2[(byte) (ebx_68 >> 8)] ^ Td1[(byte) (eax_358 >> 16)] ^ Td0[(byte) (ecx_200 >> 24)] ^ Td3[(byte) ebx_63] ^ rk[49];
        uint eax_413 = Td1[(byte) (ecx_200 >> 16)] ^ Td2[(byte) (eax_358 >> 8)] ^ Td0[(byte) (ebx_63 >> 24)] ^ Td3[(byte) ebx_68] ^ rk[50];
        uint ecx_228 = Td2[(byte) (ecx_200 >> 8)] ^ Td1[(byte) (ebx_63 >> 16)] ^ Td0[(byte) (ebx_68 >> 24)] ^ Td3[(byte) eax_358] ^ rk[51];
        uint eax_432 = Td2[(byte)(eax_413 >> 8)] ^ Td1[(byte) (ecx_228 >> 16)] ^ Td0[(byte) (ecx_215 >> 24)] ^ Td3[(byte) ecx_221] ^ rk[52];
        uint ecx_234 = Td2[(byte) (ecx_228 >> 8)] ^ Td1[(byte) (ecx_215 >> 16)] ^ Td0[(byte) (ecx_221 >> 24)] ^ Td3[(byte) eax_413] ^ rk[53];
        uint eax_451 = Td1[(byte) (ecx_221 >> 16)] ^ Td2[(byte) (ecx_215 >> 8)] ^ Td0[(byte) (eax_413 >> 24)] ^ Td3[(byte) ecx_228] ^ rk[54];
        uint edi_33 = Td2[(byte) (ecx_221 >> 8)] ^ Td3[(byte) ecx_215] ^ Td1[(byte) (eax_413 >> 16)] ^ Td0[(byte) (ecx_228 >> 24)] ^ rk[55];

        uint ecx_243 = (((byte) Td4[(byte) (edi_33 >> 16)] * 0x0001_0101u) & 0x00FF_0000u) ^ (((byte) Td4[(byte) (eax_451 >> 8)] * 0x0000_0101u) & 0x0000_FF00u);
        uint ecx_246 = ecx_243 ^ (((byte) Td4[(byte) (eax_432 >> 24)] * 0x0101_0101u) & 0xFF00_0000u) ^ (byte) Td4[(byte) ecx_234] ^ rk[56];

        a4[0] = (byte) ((ecx_246 >> 24) ^ xorKey[0]);
        a4[1] = (byte) ((ecx_246 >> 16) ^ xorKey[1]);
        a4[2] = (byte) ((ecx_246 >> 8) ^ xorKey[2]);
        a4[3] = (byte) (ecx_246 ^ xorKey[3]);

        uint edx_55 =
            (((byte) Td4[(byte) (eax_432 >> 16)] * 0x0001_0101u) & 0x00FF_0000u) ^
            (((byte) Td4[(byte) (edi_33 >> 8)] * 0x0000_0101u) & 0x0000_FF00u) ^
            (((byte) Td4[(byte) (ecx_234 >> 24)] * 0x0101_0101u) & 0xFF00_0000u) ^ rk[57];

        a4[4] = (byte) ((edx_55 >> 24) ^ xorKey[4]);
        a4[5] = (byte) ((edx_55 >> 16) ^ xorKey[5]);
        a4[6] = (byte) ((edx_55 >> 8) ^ xorKey[6]);
        a4[7] = (byte) ((Td4[(byte) eax_451] ^ rk[57]) ^ xorKey[7]);

        uint edx_61 =
            (((byte) Td4[(byte) (ecx_234 >> 16)] * 0x0001_0101u) & 0x00FF_0000u) ^
            (((byte) Td4[(byte) (eax_432 >> 8)] * 0x0000_0101u) & 0x0000_FF00u) ^
            (((byte) Td4[(byte) (eax_451 >> 24)] * 0x0101_0101u) & 0xFF00_0000u) ^ rk[58];

        
        a4[8] = (byte) ((edx_61 >> 24) ^ xorKey[8]);
        a4[9] = (byte) ((edx_61 >> 16) ^ xorKey[9]);
        a4[10] = (byte) ((edx_61 >> 8) ^ xorKey[10]);
        a4[11] = (byte) ((Td4[(byte) edi_33] ^ rk[58]) ^ xorKey[11]);

        uint edx_67 =
            (((byte) Td4[(byte) (eax_451 >> 16)] * 0x0001_0101u) & 0x00FF_0000u) ^
            (((byte) Td4[(byte) (ecx_234 >> 8)] * 0x0000_0101u) & 0x0000_FF00u) ^
            (((byte) Td4[(byte) (edi_33 >> 24)] * 0x0101_0101u) & 0xFF00_0000u) ^ rk[59];
        
        a4[12] = (byte) ((edx_67 >> 24) ^ xorKey[12]);
        a4[13] = (byte) ((edx_67 >> 16) ^ xorKey[13]);
        a4[14] = (byte) ((edx_67 >> 8) ^ xorKey[14]);
        a4[15] = (byte) ((Td4[(byte) eax_432] ^ rk[59]) ^ xorKey[15]);
    }

    private static readonly byte[] xorKey = [0xB3, 0x9C, 0x26, 0xA1, 0x0E, 0x5A, 0x3A, 0xD3, 0x19, 0xD1, 0x62, 0x28, 0x50, 0x9C, 0xB5, 0x2F];

    private static readonly uint[] rk =
        [
            0xA1E3936D, 0x91294977, 0xCF8CD4BF, 0xA8DE1C1E,
            0x334AE9AD, 0x46879115, 0x00DA2A31, 0xF1C1210E,
            0xE5A29A3C, 0xAB7635D2, 0x3BD3BFF9, 0x0D161A5D,
            0x99D890B3, 0x75CD78B8, 0x465DBB24, 0xF11B0B3F,
            0xEBFFFC58, 0x4ED4AFEE, 0x90A58A2B, 0x36C5A5A4,
            0x405B595F, 0xEC15E80B, 0x3390C39C, 0xB746B01B,
            0x3E28F779, 0xA52B53B6, 0xDE7125C5, 0xA6602F8F,
            0x8A4B7D9F, 0xAC4EB154, 0xDF852B97, 0x84D67387,
            0x9DE56208, 0x9B03A4CF, 0x7B5A7673, 0x78110A4A,
            0xF9BEE30F, 0x2605CCCB, 0x73CB9AC3, 0x5B535810,
            0x71DF3DB5, 0x06E6C6C7, 0xE059D2BC, 0x034B7C39,
            0x76E21A1C, 0xDFBB2FC4, 0x55CE5608, 0x2898C2D3,
            0x3956F631, 0x7739FB72, 0xE6BF147B, 0xE312AE85,
            0x80F4F081, 0xA95935D8, 0x8A7579CC, 0x7D5694DB,
            0xDB3A500D, 0x63C4FE36, 0x4EA5C9D3, 0xA26FDEFF
        ];

    public static byte[] AssaultFireFutureDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var ciphertext = bytes[beginOffset..];
        var plaintext = new byte[count];

        for (var offset = 0; offset < count; offset += 16)
        {
            rijndaelDecrypt(rk, ciphertext.AsSpan()[offset..(offset+16)], plaintext.AsSpan()[offset..(offset + 16)]);
        }

        return plaintext;
    }
}
