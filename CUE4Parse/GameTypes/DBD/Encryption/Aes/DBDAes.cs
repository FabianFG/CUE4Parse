using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.DBD.Encryption.Aes;

public static class DBDAes
{
    public static byte[] DbDDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        var aesKey = reader.AesKey;
        if (aesKey is null)
            throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");

        if (!isIndex) return bytes.Decrypt(aesKey);
        var key = GetDbDEncryptionKey(reader);
        var keylength = key.Length;
        if (keylength == 0) return bytes.Decrypt(aesKey);

        if (keylength == 28)
        {
            var decrypted = bytes.Decrypt(beginOffset, count, aesKey);
            TensorUtils.Xor(decrypted, key);
            return decrypted;
        }

        var decrypted2 = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, decrypted2, 0, count);
        TensorUtils.Xor(decrypted2, key);

        return decrypted2.Decrypt(aesKey);
    }

    private static byte[] GetDbDEncryptionKey(IAesVfsReader reader)
    {
        switch (reader)
        {
            case PakFileReader pak:
                return pak.Info.CustomEncryptionData;
            case IoStoreReader ioreader:
                var key = new byte[36];
                Buffer.BlockCopy(ioreader.TocResource.Header._reserved8, 0, key, 0, key.Length);
                return key;
            default:
                return [];
        }
    }
}
