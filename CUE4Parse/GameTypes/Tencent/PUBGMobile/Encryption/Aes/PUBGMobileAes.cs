using System.Security.Cryptography;
using CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.RSA;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.Aes;

public class PUBGMobileAes
{
    // Used only for index decryption
    public static byte[] PUBGMobileDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (beginOffset > bytes.Length - count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16", nameof(count));

        var keyData = (reader as PakFileReader)?.Info.CustomEncryptionData;
        if (keyData is not { Length: PUBGMobileRSA.KEY_DATA_SIZE })
            throw new ParserException($"Invalid PUBG Mobile index key data size: {keyData?.Length ?? 0}, expected {PUBGMobileRSA.KEY_DATA_SIZE}");

        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = keyData.AsSpan(0, 32).ToArray();
        aes.IV = keyData.AsSpan(32, 16).ToArray();

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(bytes, beginOffset, count);
    }
}
