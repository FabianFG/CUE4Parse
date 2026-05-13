using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.Snowbreak.Encryption.Aes;

public static class SnowbreakAes
{
    private static volatile FAesKey? _activeKey;
    private static ConditionalWeakTable<IAesVfsReader, FAesKey> _aesKeysCache = [];
    private static readonly object _lock = new();

    public static byte[] SnowbreakDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");

        var key = reader.AesKey?? throw new NullReferenceException(nameof(reader.AesKey));

        if (key.IsDefault)
            return bytes;

        if (!ReferenceEquals(_activeKey, key))
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_activeKey, key))
                {
                    _activeKey = key;
                    _aesKeysCache = [];
                }
            }
        }

        var transformedKey = _aesKeysCache.GetValue(reader, r => ConvertSnowbreakAes(r.Name, key));

        return bytes.Decrypt(transformedKey);
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
