using System;
using System.Security.Cryptography;
using CUE4Parse.UE4.VirtualFileSystem;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.GameTypes.Rennsport.Encryption.Aes;

/// <summary>
/// Reversed by Spiritovod
/// </summary>
public static class RennsportAes
{
    private static AesProvider Provider = new AesCryptoServiceProvider
    {
        Mode = CipherMode.CBC,
        Padding = PaddingMode.None,
        BlockSize = 16 * 8
    };

    private static byte[] iv0 = [0x7C, 0x8D, 0x5C, 0x6C, 0x70, 0x52, 0xA2, 0x41, 0x0C, 0xA5, 0xC3, 0x70, 0xB9, 0x9D, 0x23, 0x3B];
    private static byte[] iv1 = [0x75, 0x7C, 0xE4, 0x79, 0x62, 0xF4, 0xC7, 0x28, 0xF9, 0x89, 0x83, 0x6F];
    private static byte[] iv2 = [0xD1, 0xDC, 0x32, 0x32, 0x8E, 0xB3, 0x7F, 0xE2, 0x76, 0x49, 0xC9, 0xDC];
    //private static byte[] iv3 = [0x5E, 0x7B, 0x71, 0x28, 0x7E, 0x37, 0xDE, 0xDB, 0xAC, 0x2B, 0xEA, 0xD3];

    private static byte[] tempkey =
    [
        0x8E, 0x53, 0x68, 0xB7, 0x6B, 0x5A, 0xA3, 0xBA, 0xD8, 0xF9, 0xE0, 0x18, 0x75, 0x4C, 0x9A, 0x12,
        0x66, 0x23, 0x0D, 0xAC, 0x19, 0xA2, 0x77, 0xE4, 0xA3, 0x2C, 0xD1, 0xD9, 0x2E, 0x92, 0x45, 0x50
    ];

    public static byte[] RennsportDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader) =>
        RennsportDecrypt(bytes, beginOffset, count, isIndex, reader, false);

    public static byte[] RennsportDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, bool directoryIndex)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        // 1C AC 76 88 32 7D 56 4C A8 7E 23 11 4A 24 7D B1 4F 52 43 34 AC DF D0 EB B0 F3 91 5D D0 84 61 85
        var key = CUE4Parse.Encryption.Aes.Aes.Decrypt(tempkey, reader.AesKey);

        if (!isIndex) return Provider.CreateDecryptor(key, iv0).TransformFinalBlock(bytes, beginOffset, count);

        var output = new byte[count];
        byte[] iv = directoryIndex ? iv2 : iv1;
        var cipher = new GcmBlockCipher(new AesEngine());
        cipher.Init(false, new ParametersWithIV(new KeyParameter(key), iv));
        cipher.ProcessBytes(bytes, 0, count, output, 0);
        return output;
    }
}
