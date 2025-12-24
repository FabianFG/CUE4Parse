using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.BB3.Encryption.Aes;

public static class BloodBowl3Aes
{
    public static byte[] BloodBowl3Decrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        if (reader.AesKey.Key.Length == 32) reader.AesKey = new FAesKey(reader.AesKey.Key[..16], true);
        return AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);
    }
}
