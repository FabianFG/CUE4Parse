using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using Blake3;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.SilverPalace.Encryption;

public static class SilverPalaceAes
{
    // Only the first 16 bytes of this are actually used.
    private static ReadOnlySpan<byte> XorKey =>
    [
        0x67, 0xFD, 0x3C, 0x26, 0x7A, 0xE4, 0xF3, 0xAB, 0x99, 0xB6,
        0xA9, 0xC6, 0x37, 0x5B, 0xF8, 0x62, 0xE3, 0x44, 0xCE, 0xB8,
        0x37, 0x57, 0xF8, 0xBC, 0x6E, 0x4B, 0x5A, 0x12, 0x06, 0x40,
        0x6A, 0x7F, 0xD6, 0xFE, 0x2F, 0xD0, 0x85, 0xCF, 0x82, 0xA5,
        0xC1, 0x33, 0xAA, 0x93, 0x33, 0xF1, 0x21, 0x54, 0x74, 0x37,
        0xCA, 0xE6, 0xD4, 0xFC, 0x75, 0xE2, 0x96, 0x14, 0x12, 0xEE,
        0x43, 0x1E, 0xA5, 0xF0
    ];

    // Used starting from CBT2
    public static byte[] SilverPalaceDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var key = reader.AesKey;
        var pakName = Path.GetFileNameWithoutExtension(reader.Name).ToLower();

        var pakKey = (stackalloc byte[KeySize]);
        {
            using var hash = Hasher.New();
            hash.Update(key.Key);
            hash.Update(Encoding.UTF8.GetBytes(pakName));
            hash.Finalize(pakKey);
        }

        var data = bytes.AsSpan(beginOffset, count).ToArray();
        DecryptCustomAes(data, pakKey);
        return data;
    }

    private static void DecryptCustomAes(Span<byte> data, Span<byte> key)
    {
        DecryptData(data, key);
        for (int i = 0; i < data.Length; i++)
            data[i] ^= XorKey[i & 0xF];
    }

    private const int BlockSize = 16;
    private const int KeySize = 32;
    private const int RoundCount = 14;

    private static void DecryptData(Span<byte> contents, ReadOnlySpan<byte> key)
    {
        Span<uint> dKey = stackalloc uint[4 * (RoundCount + 1)];
        AesDecryptExpand(dKey, key);

        for (int offset = 0; offset < contents.Length; offset += BlockSize)
        {
            Span<byte> block = contents.Slice(offset, BlockSize);
            DecryptBlock(block, dKey);
        }
    }

    private static void DecryptBlock(Span<byte> block, Span<uint> dKey)
    {
        uint s0 = ReadU32Le(block.Slice(0, 4)) ^ dKey[0];
        uint s1 = ReadU32Le(block.Slice(4, 4)) ^ dKey[1];
        uint s2 = ReadU32Le(block.Slice(8, 4)) ^ dKey[2];
        uint s3 = ReadU32Le(block.Slice(12, 4)) ^ dKey[3];

        for (int round = 1; round < RoundCount; round++)
        {
            uint t0 = AesDec(s0, s3, s2, s1);
            uint t1 = AesDec(s1, s0, s3, s2);
            uint t2 = AesDec(s2, s1, s0, s3);
            uint t3 = AesDec(s3, s2, s1, s0);

            s0 = t0 ^ dKey[4 * round + 0];
            s1 = t1 ^ dKey[4 * round + 1];
            s2 = t2 ^ dKey[4 * round + 2];
            s3 = t3 ^ dKey[4 * round + 3];
        }

        uint u0 = AesDecLast(s0, s3, s2, s1) ^ dKey[4 * RoundCount + 0];
        uint u1 = AesDecLast(s1, s0, s3, s2) ^ dKey[4 * RoundCount + 1];
        uint u2 = AesDecLast(s2, s1, s0, s3) ^ dKey[4 * RoundCount + 2];
        uint u3 = AesDecLast(s3, s2, s1, s0) ^ dKey[4 * RoundCount + 3];

        WriteU32Le(block.Slice(0, 4), u0);
        WriteU32Le(block.Slice(4, 4), u1);
        WriteU32Le(block.Slice(8, 4), u2);
        WriteU32Le(block.Slice(12, 4), u3);
    }

    private static void AesEncryptExpand(Span<uint> eKey, ReadOnlySpan<byte> key)
    {
        eKey[0] = ReadU32Le(key.Slice(0, 4));
        eKey[1] = ReadU32Le(key.Slice(4, 4));
        eKey[2] = ReadU32Le(key.Slice(8, 4));
        eKey[3] = ReadU32Le(key.Slice(12, 4));
        eKey[4] = ReadU32Le(key.Slice(16, 4));
        eKey[5] = ReadU32Le(key.Slice(20, 4));
        eKey[6] = ReadU32Le(key.Slice(24, 4));
        eKey[7] = ReadU32Le(key.Slice(28, 4));

        int baseIdx = 0;
        for (int index = 0; index < RoundCount / 2; index++, baseIdx += 8)
        {
            eKey[baseIdx + 8] = eKey[baseIdx + 0] ^ RotateRight(AesEncryptMix(eKey[baseIdx + 7]), 8) ^ _rcon[index];
            eKey[baseIdx + 9] = eKey[baseIdx + 1] ^ eKey[baseIdx + 8];
            eKey[baseIdx + 10] = eKey[baseIdx + 2] ^ eKey[baseIdx + 9];
            eKey[baseIdx + 11] = eKey[baseIdx + 3] ^ eKey[baseIdx + 10];

            if (index < RoundCount / 2 - 1)
            {
                eKey[baseIdx + 12] = eKey[baseIdx + 4] ^ AesEncryptMix(eKey[baseIdx + 11]);
                eKey[baseIdx + 13] = eKey[baseIdx + 5] ^ eKey[baseIdx + 12];
                eKey[baseIdx + 14] = eKey[baseIdx + 6] ^ eKey[baseIdx + 13];
                eKey[baseIdx + 15] = eKey[baseIdx + 7] ^ eKey[baseIdx + 14];
            }
        }
    }

    private static void AesDecryptExpand(Span<uint> dKey, ReadOnlySpan<byte> key)
    {
        Span<uint> eKey = stackalloc uint[4 * (RoundCount + 1)];
        AesEncryptExpand(eKey, key);

        int e = 4 * RoundCount;
        int d = 0;

        dKey[d++] = eKey[e++];
        dKey[d++] = eKey[e++];
        dKey[d++] = eKey[e++];
        dKey[d++] = eKey[e];
        e -= 7;

        for (int round = 1; round < RoundCount; round++)
        {
            dKey[d++] = AesDecryptMix(eKey[e + 0]);
            dKey[d++] = AesDecryptMix(eKey[e + 1]);
            dKey[d++] = AesDecryptMix(eKey[e + 2]);
            dKey[d++] = AesDecryptMix(eKey[e + 3]);
            e -= 4;
        }

        dKey[d++] = eKey[e + 0];
        dKey[d++] = eKey[e + 1];
        dKey[d++] = eKey[e + 2];
        dKey[d] = eKey[e + 3];
    }

    private static uint AesEncryptMix(uint x)
    {
        return _sBox[(byte) (x >> 0)] |
               ((uint) _sBox[(byte) (x >> 8)] << 8) |
               ((uint) _sBox[(byte) (x >> 16)] << 16) |
               ((uint) _sBox[(byte) (x >> 24)] << 24);
    }

    private static uint AesDecryptMix(uint x)
    {
        return _itBox[_sBox[(byte) (x >> 0)]] ^
               RotateLeft(_itBox[_sBox[(byte) (x >> 8)]], 8) ^
               RotateLeft(_itBox[_sBox[(byte) (x >> 16)]], 16) ^
               RotateLeft(_itBox[_sBox[(byte) (x >> 24)]], 24);
    }

    private static uint AesDec(uint a, uint b, uint c, uint d)
    {
        return _itBox[(byte) (a >> 0)] ^
               RotateLeft(_itBox[(byte) (b >> 8)], 8) ^
               RotateLeft(_itBox[(byte) (c >> 16)], 16) ^
               RotateLeft(_itBox[(byte)(d >> 24)], 24);
    }

    private static uint AesDecLast(uint a, uint b, uint c, uint d)
    {
        return _isBox[(byte) (a >> 0)] |
               ((uint) _isBox[(byte) (b >> 8)] << 8) |
               ((uint) _isBox[(byte) (c >> 16)] << 16) |
               ((uint) _isBox[(byte) (d >> 24)] << 24);
    }

    private static uint RotateRight(uint x, int n)
        => BitOperations.RotateRight(x, n);
    private static uint RotateLeft(uint x, int n)
        => BitOperations.RotateLeft(x, n);

    private static uint ReadU32Le(ReadOnlySpan<byte> src)
        => BinaryPrimitives.ReadUInt32LittleEndian(src);
    private static void WriteU32Le(Span<byte> dst, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(dst, value);

    private static readonly byte[] _rcon =
    [
        0xFB, 0xCE, 0x33, 0xA8, 0x33, 0xE6, 0x36, 0x00
    ];

    private static readonly byte[] _sBox =
    [
        0x1E, 0x66, 0x23, 0x53, 0x6B, 0x57, 0xC7, 0x85, 0x78, 0x77, 0xEE, 0x8A, 0x31, 0x99, 0xEF, 0xB2, 0x20, 0x0E,
        0x9C, 0x3D, 0x74, 0x8F, 0x7F, 0x98, 0xF4, 0xA6, 0xDC, 0xC3, 0x44, 0xF3, 0x22, 0x63, 0xC2, 0x13, 0x83, 0xB4,
        0x80, 0x18, 0xE6, 0x6D, 0xAC, 0x38, 0x26, 0xD8, 0x46, 0x42, 0xDA, 0x1C, 0x3C, 0x67, 0x75, 0xC8, 0xED, 0x0C,
        0xB7, 0xC9, 0xB9, 0xE9, 0xD3, 0x70, 0x1B, 0x16, 0x24, 0x2F, 0x33, 0x2E, 0x51, 0xFD, 0xA3, 0x3F, 0xFF, 0xEB,
        0xE7, 0x6C, 0xFB, 0x7B, 0xB0, 0xC5, 0x12, 0x71, 0xD1, 0x2C, 0x4F, 0x76, 0xA7, 0xAB, 0x9B, 0xA0, 0x19, 0x92,
        0x27, 0x11, 0x47, 0x34, 0x9D, 0xAA, 0xF7, 0xFE, 0x21, 0xBB, 0xE5, 0x68, 0x0D, 0xD5, 0xDF, 0xFC, 0xCE, 0x60,
        0x5D, 0x95, 0xA1, 0x32, 0x09, 0xA9, 0x2D, 0xD0, 0x00, 0x05, 0x06, 0x61, 0x3E, 0xCC, 0xF2, 0x72, 0x28, 0x40,
        0xD2, 0xE8, 0x7E, 0xF1, 0x3A, 0x0F, 0x07, 0x97, 0x90, 0x79, 0xD7, 0x35, 0xAF, 0xFA, 0x82, 0x91, 0x6E, 0x08,
        0x56, 0xBF, 0xB1, 0x7C, 0x15, 0x8C, 0x5E, 0x9F, 0x52, 0xEC, 0x88, 0x17, 0xC4, 0x50, 0xBE, 0x04, 0x03, 0xCA,
        0xB6, 0x73, 0x1A, 0xE0, 0xAE, 0x69, 0x2A, 0x4A, 0x4C, 0x93, 0x58, 0x8E, 0xB3, 0x5C, 0x8D, 0x14, 0xBA, 0x5B,
        0xBD, 0xF5, 0x1D, 0x49, 0x86, 0xC1, 0x87, 0xC0, 0xDD, 0x81, 0x65, 0xD4, 0x4D, 0x94, 0xF9, 0xA4, 0x02, 0x43,
        0xDB, 0x54, 0xCD, 0x39, 0x45, 0xE1, 0x9A, 0x5F, 0x25, 0xC6, 0x96, 0x55, 0x7A, 0x10, 0x0B, 0x64, 0x62, 0x9E,
        0x41, 0xD6, 0xE3, 0xA5, 0xEA, 0x5A, 0x3B, 0xF8, 0xA2, 0xCB, 0xB5, 0x59, 0x89, 0xD9, 0x36, 0x6F, 0xF6, 0x6A,
        0xBC, 0xB8, 0x30, 0xE2, 0xF0, 0x37, 0x2B, 0x48, 0x4B, 0xE4, 0x1F, 0x0A, 0x29, 0xAD, 0xDE, 0x01, 0x7D, 0xCF,
        0xA8, 0x84, 0x4E, 0x8B
    ];

    private static readonly byte[] _isBox =
    [
        0x74, 0xF9, 0xC4, 0xA0, 0x9F, 0x75, 0x76, 0x84, 0x8F, 0x70,
        0xF5, 0xD4, 0x35, 0x66, 0x11, 0x83, 0xD3, 0x5B, 0x4E, 0x21,
        0xB1, 0x94, 0x3D, 0x9B, 0x25, 0x58, 0xA4, 0x3C, 0x2F, 0xB6,
        0x00, 0xF4, 0x10, 0x62, 0x1E, 0x02, 0x3E, 0xCE, 0x2A, 0x5A,
        0x7C, 0xF6, 0xA8, 0xF0, 0x51, 0x72, 0x41, 0x3F, 0xEC, 0x0C,
        0x6F, 0x40, 0x5D, 0x89, 0xE6, 0xEF, 0x29, 0xC9, 0x82, 0xDE,
        0x30, 0x13, 0x78, 0x45, 0x7D, 0xD8, 0x2D, 0xC5, 0x1C, 0xCA,
        0x2C, 0x5C, 0xF1, 0xB7, 0xA9, 0xF2, 0xAA, 0xC0, 0xFE, 0x52,
        0x9D, 0x42, 0x98, 0x03, 0xC7, 0xD1, 0x90, 0x05, 0xAC, 0xE3,
        0xDD, 0xB3, 0xAF, 0x6C, 0x96, 0xCD, 0x6B, 0x77, 0xD6, 0x1F,
        0xD5, 0xBE, 0x01, 0x31, 0x65, 0xA7, 0xE9, 0x04, 0x49, 0x27,
        0x8E, 0xE7, 0x3B, 0x4F, 0x7B, 0xA3, 0x14, 0x32, 0x53, 0x09,
        0x08, 0x87, 0xD2, 0x4B, 0x93, 0xFA, 0x80, 0x16, 0x24, 0xBD,
        0x8C, 0x22, 0xFD, 0x07, 0xB8, 0xBA, 0x9A, 0xE4, 0x0B, 0xFF,
        0x95, 0xB0, 0xAD, 0x15, 0x86, 0x8D, 0x59, 0xAB, 0xC1, 0x6D,
        0xD0, 0x85, 0x17, 0x0D, 0xCC, 0x56, 0x12, 0x5E, 0xD7, 0x97,
        0x57, 0x6E, 0xE0, 0x44, 0xC3, 0xDB, 0x19, 0x54, 0xFC, 0x71,
        0x5F, 0x55, 0x28, 0xF7, 0xA6, 0x8A, 0x4C, 0x92, 0x0F, 0xAE,
        0x23, 0xE2, 0xA2, 0x36, 0xEB, 0x38, 0xB2, 0x63, 0xEA, 0xB4,
        0x9E, 0x91, 0xBB, 0xB9, 0x20, 0x1B, 0x9C, 0x4D, 0xCF, 0x06,
        0x33, 0x37, 0xA1, 0xE1, 0x79, 0xC8, 0x6A, 0xFB, 0x73, 0x50,
        0x7E, 0x3A, 0xBF, 0x67, 0xD9, 0x88, 0x2B, 0xE5, 0x2E, 0xC6,
        0x1A, 0xBC, 0xF8, 0x68, 0xA5, 0xCB, 0xED, 0xDA, 0xF3, 0x64,
        0x26, 0x48, 0x7F, 0x39, 0xDC, 0x47, 0x99, 0x34, 0x0A, 0x0E,
        0xEE, 0x81, 0x7A, 0x1D, 0x18, 0xB5, 0xE8, 0x60, 0xDF, 0xC2,
        0x8B, 0x4A, 0x69, 0x43, 0x61, 0x46
    ];

    private static readonly uint[] _itBox =
    [
        288553390u, 2579067049u, 763608788u, 2355222426u, 776014843u, 440397984u, 120122290u, 3689859193u,
        2660342555u, 1023860118u, 3991215329u, 2639474228u, 3964831245u, 3086515026u, 3151862254u, 3939366739u,
        2893025566u, 63092015u, 2485848313u, 1890988757u, 935087732u, 1799248025u, 3023752829u, 41234371u,
        1552029421u, 517320253u, 2689987490u, 3208103795u, 317738113u, 111112542u, 0u, 3873969647u, 2966458592u,
        2613862250u, 3533106868u, 370807324u, 2838353263u, 1676797112u, 902390199u, 147831841u, 1230680542u,
        4042393587u, 3557400554u, 3403428311u, 1303441219u, 723308426u, 4250959779u, 2720062561u, 240176511u,
        1952214088u, 3835509292u, 4134368941u, 971801355u, 2764025151u, 1076008723u, 323475053u, 685669029u,
        1389550482u, 3787521629u, 3542185048u, 3412831035u, 2915535858u, 1701746150u, 3511966619u, 1113045200u,
        3910091388u, 82468509u, 646887386u, 3297574056u, 1338359936u, 266819475u, 853641733u, 3251714265u,
        227702864u, 3741619940u, 3703972811u, 3256061430u, 28809964u, 2828685187u, 1353184337u, 945494503u,
        3765920945u, 526529745u, 488053522u, 815048134u, 3127509762u, 1191869601u, 658058550u, 4164795346u,
        1729870373u, 3459673930u, 564550760u, 3844776128u, 4186579262u, 2099530373u, 2129067946u, 3366526484u,
        203809468u, 2336832552u, 3650873274u, 2522752826u, 1593260334u, 185403662u, 3227951669u, 2867814464u,
        3175278768u, 694804553u, 741614648u, 2768779219u, 1251476721u, 2510066197u, 1261411869u, 2383738969u,
        2670068215u, 2021232372u, 2440481928u, 2632234200u, 3715217703u, 1537932639u, 1399144830u, 1483229296u,
        3338261355u, 2809993232u, 3004310991u, 1511876531u, 2226023355u, 4158319681u, 2331944644u, 1468997603u,
        1138762300u, 2206629897u, 1839278535u, 3045938321u, 824393514u, 1691946762u, 1925389590u, 158869197u,
        1446544655u, 1165972322u, 2744600205u, 1613975959u, 1018251130u, 4080055004u, 2548678102u, 3455375973u,
        2290845959u, 366520115u, 3374220536u, 179999714u, 4068943920u, 2976320012u, 3504587127u, 2180231114u,
        2136040774u, 1978398372u, 2090061929u, 2798289660u, 620468249u, 2151953702u, 1982415755u, 2006899047u,
        4019204898u, 2047648055u, 3663286933u, 480281086u, 4095236462u, 3820343710u, 1787413109u, 3196083615u,
        906744984u, 804688151u, 1636092795u, 601060267u, 4227796733u, 3058688446u, 3114841645u, 2184256229u,
        1360031421u, 1766553434u, 3995576782u, 1722556617u, 1813426987u, 2591802758u, 4047871263u, 1064563285u,
        2466505547u, 715871590u, 2428589668u, 880737115u, 277177154u, 625738485u, 1275557295u, 2043548696u,
        1876166148u, 2075868123u, 4121936770u, 861278441u, 2302415851u, 1761406390u, 976107044u, 3598495785u,
        4199060497u, 2272059028u, 1895934009u, 1852021992u, 1507840668u, 3282310938u, 2409849525u, 538035844u,
        1186850381u, 1598071746u, 2233069911u, 1442459680u, 3170202204u, 3791801202u, 2947720241u, 1053059257u,
        1563790337u, 436028815u, 999926984u, 4272081548u, 1223502642u, 2461301159u, 3586000134u, 2874735276u,
        1154009486u, 90031217u, 4279586912u, 3620416197u, 2715969870u, 1100287487u, 2919579357u, 1414376140u,
        2551114309u, 3309004356u, 3343796615u, 342834655u, 3881799427u, 1316117100u, 1648787028u, 407560035u,
        4242007375u, 1937016826u, 3481400742u, 3904428176u, 461924940u, 577038663u, 2379631990u, 3626794326u,
        397248752u, 2997206819u, 3089050817u, 3736275976u, 3950669247u, 2262516856u, 3426959497u
    ];
}
