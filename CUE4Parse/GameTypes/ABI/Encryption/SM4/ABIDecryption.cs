using CUE4Parse.GameTypes.ABI.UE4.Lua;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.ABI.Encryption.SM4;

public static class ABIDecryption
{
    public static readonly string[] encryptedFiles = ["ini", "lua", "uasset", "umap"];
    private static readonly byte[] iniDecryptKey = [0x97, 0x67, 0x87, 0xDE, 0xEA, 0x18, 0x47, 0x0D, 0xA8, 0x07, 0x90, 0xB6, 0x45, 0x27, 0x23, 0x14];
    private static readonly byte[] uassetDecryptKey37 = [0x43, 0x23, 0x07, 0x67, 0x19, 0xAB, 0xAC, 0xEF, 0xFE, 0x3C, 0xB3, 0xA8, 0x71, 0x57, 0x12, 0x40];
    private static readonly byte[] uassetDecryptKey38 = [0x3C, 0x17, 0x08, 0xD5, 0xBD, 0x80, 0xD8, 0x15, 0x62, 0x37, 0xDD, 0x59, 0x15, 0x1C, 0x28, 0xA8];
    private static readonly byte[] uassetDecryptKey39 = [0xDF, 0x2E, 0xBD, 0x77, 0xDE, 0xAB, 0xDC, 0x56, 0xC2, 0x29, 0xD6, 0xD9, 0xA4, 0x99, 0xA8, 0xAC];

    #region Mobile
    private static readonly byte[] pakInfoMobileKey = [0x76, 0x69, 0xF3, 0x85, 0x02, 0xC1, 0xC4, 0xF6, 0xA7, 0xC4, 0x0B, 0x57, 0x35, 0x6B, 0x68, 0x9E];
    private static readonly byte[] pakIndexMobileKey = [0xF3, 0x7F, 0x02, 0xC1, 0x8B, 0x29, 0x5E, 0x5B, 0xC9, 0x8C, 0xA3, 0xD6, 0x38, 0x97, 0x0B, 0xEC];
    private static readonly byte[] iniDecryptMobileKey = [0x0D, 0x46, 0xCB, 0x87, 0x0B, 0x4B, 0x4C, 0x4D, 0x30, 0xB3, 0xF0, 0x72, 0xDA, 0x5C, 0x1D, 0x1C];

    private static readonly byte[] uassetDecryptMobileKey38 = [0x43, 0x23, 0x07, 0x67, 0x19, 0xAB, 0xAC, 0xEE, 0xFE, 0x3C, 0xB3, 0xAB, 0x71, 0x58, 0x12, 0x40];
    private static readonly byte[] uassetDecryptMobileKey39 = [0x43, 0x23, 0x07, 0x67, 0x19, 0xAB, 0xAC, 0xF0, 0xFE, 0x3C, 0xB3, 0xAC, 0x71, 0x58, 0x12, 0x40];
    #endregion

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
            if (reader.Game is GAME_ArenaBreakoutMobile)
            {
                Sm4Helper.Decrypt(ref output, pakIndexMobileKey, SM4Mode.None, SboxMode.None);
            }
            else
            {
                if (reader.AesKey == null)
                    throw new NullReferenceException("reader.AesKey");

                Sm4Helper.Decrypt(ref output, reader.AesKey.Key, SM4Mode.A, SboxMode.None);
            }

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

        // For PC base is 0x03000000, for mobile 0x04000000
        (byte[] currentKey, SM4Mode mode, SboxMode sboxMode) = magic switch
        {
            0x03000337 or 0x04000337 => (uassetDecryptKey37, SM4Mode.C, SboxMode.Mode37),
            0x03000338 => (uassetDecryptKey38, SM4Mode.C, SboxMode.Mode38),
            0x03000339 => (uassetDecryptKey39, SM4Mode.D, SboxMode.Mode39),
            0x04000338 => (uassetDecryptMobileKey38, SM4Mode.C, SboxMode.Mode38Mobile),
            0x04000339 => (uassetDecryptMobileKey39, SM4Mode.C, SboxMode.Mode39Mobile),
            _ => throw new ParserException($"FilePackageSummary magic is different 0x{magic:X} (encryption is not supported)")
        };

        var encryptedLength = BitConverter.ToUInt16(bytes, 6);
        var unencryptedLength = encryptedLength + uassetMagicLength;
        var output = new byte[bytes.Length];
        Buffer.BlockCopy(uassetMagic, 0, output, 0, uassetMagicLength);
        Buffer.BlockCopy(bytes, unencryptedLength, output, unencryptedLength, bytes.Length - unencryptedLength);

        var encryptedBlock = new byte[encryptedLength];
        Buffer.BlockCopy(bytes, uassetMagicLength, encryptedBlock, 0, encryptedLength);

        Sm4Helper.Decrypt(ref encryptedBlock, currentKey, mode, sboxMode);

        Buffer.BlockCopy(encryptedBlock, 0, output, uassetMagicLength, encryptedLength);
        return output;
    }

    public static byte[] AbiDecryptIni(byte[] bytes, EGame game)
    {
        if (bytes.Length < 8)
            throw new ArgumentException("ini file must be at least 8 bytes", nameof(bytes));
        if (bytes is not [0x1b, _, 0x55, ..])
            return bytes;

        var key = game is GAME_ArenaBreakoutMobile ? iniDecryptMobileKey : iniDecryptKey;
        var mode = game is GAME_ArenaBreakoutMobile ? SM4Mode.None : (SM4Mode) bytes[3];

        var iniLength = BitConverter.ToInt32(bytes, 4);
        var length = iniLength.Align(16);

        var output = new byte[length];
        Buffer.BlockCopy(bytes, 8, output, 0, length);
        Sm4Helper.Decrypt(ref output, key, mode, SboxMode.None);

        return iniLength != length ? output.SubByteArray(iniLength) : output;
    }

    public static byte[] AbiDecryptLua(byte[] bytes, EGame game)
    {
        if (bytes.Length < 12)
            throw new ArgumentException("Lua file must be at least 12 bytes", nameof(bytes));

        var magic = BitConverter.ToUInt32(bytes);
        if (magic != 0x4d41551b)
            return bytes;

        var decrypted = ABILuaReader.DecryptLuaBytecode(bytes, game is GAME_ArenaBreakoutMobile);

        return decrypted;
    }

    public static void DecryptAbiMobilePakInfo(byte[] data) => Sm4Helper.Decrypt(ref data, pakInfoMobileKey, SM4Mode.None, SboxMode.None);
}
