using System;
using System.Reflection;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace CUE4Parse.GameTypes.ABI.Encryption.Aes;

public static class ABIDecryption
{
    private static readonly byte[] iniDecryptKey = [0x97, 0x67, 0x87, 0xDE, 0xEA, 0x18, 0x47, 0x0D, 0xA8, 0x07, 0x90, 0xB6, 0x45, 0x27, 0x23, 0x14];
    private static readonly byte[] uassetDecryptKey = [0x43, 0x23, 0x07, 0x67, 0x19, 0xAB, 0xAC, 0xEF, 0xFE, 0x3C, 0xB3, 0xA8, 0x71, 0x57, 0x12, 0x40];
    private static readonly byte[] uassetMagic = [0xc1, 0x83, 0x2a, 0x9e, 0xf9, 0xff, 0xff, 0xff];
    private const int uassetMagicLength = 8;

    public static byte[] ABIDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");

        var output = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, output, 0, count);

        if (isIndex)
        {
            if (reader.AesKey == null) throw new NullReferenceException("reader.AesKey");
            Sm4Helper.Decrypt(reader.AesKey.Key, ref output, SM4Mode.A);
            return output;
        }

        for (var i = 0; i < count; i++)
        {
            if (output[i] != 0 && output[i] != 0x93)
                output[i] ^= 0x93;
        }

        return output;
    }

    public static byte[] AbiDecryptPackageSummary(byte[] bytes)
    {
        var magic = BitConverter.ToUInt32(bytes);
        if (magic != 0x03000337) throw new ParserException($"FilePackageSummary magic is different {magic:X}");

        var encryptedLength = BitConverter.ToUInt16(bytes, 6);
        var unencryptedLength = encryptedLength + uassetMagicLength;
        var output = new byte[bytes.Length];
        Buffer.BlockCopy(uassetMagic, 0, output, 0, uassetMagicLength);
        Buffer.BlockCopy(bytes, unencryptedLength, output, unencryptedLength, bytes.Length-unencryptedLength);

        var encryptedBlock = new byte[encryptedLength];
        Buffer.BlockCopy(bytes, uassetMagicLength, encryptedBlock, 0, encryptedLength);

        Sm4SboxSwitch.SetTo37();
        Sm4Helper.Decrypt(uassetDecryptKey, ref encryptedBlock, SM4Mode.C);
        Buffer.BlockCopy(encryptedBlock, 0, output, uassetMagicLength, encryptedLength);
        return output;
    }

    public static byte[] AbiDecryptIni(byte[] bytes)
    {
        if (bytes.Length < 8) throw new ArgumentException("ini file must be at least 8 bytes", nameof(bytes));

        if (bytes[0] != 0x1b || bytes[2] != 0x55 || bytes[3] != 0x41)
            return bytes;

        var iniLength = BitConverter.ToInt32(bytes, 4);
        var length = iniLength.Align(16);

        var output = new byte[length];
        Buffer.BlockCopy(bytes, 8, output, 0, length);

        Sm4SboxSwitch.SetTo48();
        Sm4Helper.Decrypt(iniDecryptKey, ref output, SM4Mode.A);
        return iniLength != length ? output.SubByteArray(iniLength) : output;
    }

    public static byte[] AbiDecryptLua(byte[] bytes)
    {
        if (bytes.Length < 8) throw new ArgumentException("lua file must be at least 8 bytes", nameof(bytes));

        var magic = BitConverter.ToUInt32(bytes);
        if (magic == 0x4d41551b) return bytes; // lua_dump, no idea how to decrypt
        if (bytes[0] != 0x1b || bytes[2] != 0x41 || bytes[3] != 0x4d)
            return bytes;
        var iniLength = bytes.Length - 8;
        var length = (iniLength >> 4) << 4;

        var output = new byte[length];
        Buffer.BlockCopy(bytes, 8, output, 0, length);

        Sm4SboxSwitch.SetTo48();
        Sm4Helper.Decrypt(Sm4Helper.GetKey(iniDecryptKey, SM4Mode.Lua), ref output, SM4Mode.A);
        return output;
    }
}

public enum SM4Mode : byte
{
    A = 0x41,
    B = 0x42,
    C = 0x43,
    Lua = 0xFF
}

public static class Sm4Helper
{
    static readonly byte[] TableA =
    [
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
        0x20, 0x21, 0x32, 0x43, 0x54, 0x65, 0x16, 0x17,
        0x48, 0x49, 0x6A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
    ];

    static readonly byte[] TableB =
    [
        0x9e, 0xa0, 0x7f, 0x1b, 0x76, 0x4f, 0x21, 0xf3,
        0xb2, 0x1a, 0xec, 0x41, 0x13, 0xd2, 0x66, 0xe7,
        0x29, 0x26, 0x68, 0x8c, 0x5e, 0xf1, 0x60, 0x6b,
        0x75, 0x02, 0xc1, 0x48, 0xd6, 0xe7, 0x93, 0xb7,
        0x06, 0x99, 0x97, 0x69, 0x3b, 0xce, 0xb3, 0x85,
        0x57, 0x5b, 0x72, 0xc1, 0x93, 0xa3, 0xa0, 0xf6,
        0x38, 0x35, 0xc9, 0x0b, 0x22, 0xa3, 0x36, 0x8b,
        0x9c, 0x2f, 0xc3, 0x05, 0xc4, 0xe8, 0xa7, 0x66
    ];

    static readonly byte[] TableC =
    [
        0x6C, 0xDE, 0x9A, 0x5C, 0x12, 0x83, 0x07, 0x3A,
        0xAB, 0x92, 0xCB, 0x3C, 0xEB, 0x1E, 0x4A, 0x3E,
        0xEE, 0x5F, 0xD0, 0x7F, 0x2E, 0xDE, 0x10, 0x81,
        0x31, 0xE0, 0x51, 0x83, 0xF4, 0xA4, 0xD6, 0x09,
        0x7A, 0xAC, 0xDF, 0x8E, 0xC1, 0x32, 0x77, 0x27,
        0x98, 0x47, 0xB8, 0x29, 0x5C, 0xCD, 0xFF, 0x32,
        0xA3, 0xD5, 0x46, 0x79, 0x28, 0x5B, 0x8D, 0x3C,
        0x6F, 0xE0, 0x8F, 0x00, 0x71, 0xE2, 0x92, 0x03
    ];

    static readonly byte[] TableLua =
    [
        0x22,0x32,0x42,0x52, 0x62,0x63,0x53,0x43,
        0x33,0x23,0x13,0x54, 0x55,0x56,0x51,0x31,
        0x11,0x34,0xF3,0x55, 0x33,0xE4,0xE3,0xF4,
        0xF3,0xD3,0xA3,0xA4, 0xA4,0xA7,0xB7,0xD6,
        0xAA,0xA3,0xEA,0xF7, 0xC3,0xC8,0xE1,0xAA,
        0xF3,0xE3,0xB1,0xF2, 0xA3,0xE4,0xAA,0xEB,
        0x13,0x14,0x23,0x24, 0xA3,0xA1,0xF1,0xE2,
        0xAA,0xF3,0xD3,0xD4, 0xD1,0xAD,0xAB,0x11
    ];

    public static byte[] GetKey(byte[] key, SM4Mode mode)
    {
        if (key is not { Length: >= 16 })
            throw new ArgumentException("SM4 key must be at least 16 bytes (128 bits).");

        var table = mode switch
        {
            SM4Mode.A => TableA,
            SM4Mode.B => TableB,
            SM4Mode.C => TableC,
            SM4Mode.Lua  => TableLua,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
        var res = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            res[i] = table[key[i] & 0x3F];
        }
        return res;
    }

    public static void Decrypt(byte[] key, ref byte[] data, SM4Mode mode)
    {
        var engine = new SM4Engine();
        engine.Init(false, new KeyParameter(GetKey(key, mode)));
        for (var i = 0; i < data.Length; i += 16)
        {
            engine.ProcessBlock(data, i, data, i);
        }
    }
}

public static class Sm4SboxSwitch
{
    private static readonly object Gate = new();

    private static readonly FieldInfo SboxField =
        typeof(SM4Engine).GetField("Sbox", BindingFlags.NonPublic | BindingFlags.Static) ??
        throw new MissingFieldException("SM4Engine.Sbox not found");

    private static readonly byte[] SboxRef = (byte[])SboxField.GetValue(null)!;

    private static void SetTo(byte value)
    {
        lock (Gate) SboxRef[255] = value;
    }

    public static void SetTo37() => SetTo(0x37);
    public static void SetTo48() => SetTo(0x48);
}
