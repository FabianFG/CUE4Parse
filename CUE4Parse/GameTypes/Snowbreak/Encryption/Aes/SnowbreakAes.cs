using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.Snowbreak.Encryption.Aes;

public static class SnowbreakAes
{
    private static Dictionary<IAesVfsReader, FAesKey> _aesKeys = new();

    public static byte[] SnowbreakDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        if (!_aesKeys.TryGetValue(reader, out var key))
        {
            key = ConvertSnowbreakAes(reader.Name, reader.AesKey);
            lock(_aesKeys)
            {
                _aesKeys[reader] = key;
            }
        }

        return bytes.Decrypt(key);
    }

    private static FAesKey ConvertSnowbreakAes(string name, FAesKey key)
    {
        var pakName = System.IO.Path.GetFileNameWithoutExtension(name).ToLower();
        var pakNameBytes = Encoding.ASCII.GetBytes(pakName);
        var md5Name = MD5.HashData(pakNameBytes);

        var md5AsString = Convert.ToHexString(md5Name).ToLower();
        var md5StrBytes = Encoding.ASCII.GetBytes(md5AsString);

        using var aesEnc = System.Security.Cryptography.Aes.Create();
        aesEnc.Mode = CipherMode.ECB;
        aesEnc.Key = key.Key;

        var newKey = new byte[32];
        aesEnc.EncryptEcb(md5StrBytes, newKey, PaddingMode.None);

        return new FAesKey(newKey);
    }
}
