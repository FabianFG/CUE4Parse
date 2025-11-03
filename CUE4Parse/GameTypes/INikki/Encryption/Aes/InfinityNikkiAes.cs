using System;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.INikki.Encryption.Aes;

public static class InfinityNikkiAes
{
    private const int AesBlockSize = 16;
    public static byte[] InfinityNikkiDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var key = reader.AesKey;
        var data = AesProvider.Decrypt(bytes, beginOffset, count, key);

        for (var i = 0; i < data.Length; i += AesBlockSize)
        {
            data[i] ^= key.Key[^1];
            if (data.Length >= i + AesBlockSize)
                data[i + AesBlockSize - 1] ^= key.Key[0];
        }

        return data;
    }
}
