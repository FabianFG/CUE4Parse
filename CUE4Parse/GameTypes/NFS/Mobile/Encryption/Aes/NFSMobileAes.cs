using System;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.NFS.Mobile.Encryption.Aes;

public static class NFSMobileAes
{
    public static byte[] NFSMobileDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        if (reader is PakFileReader)
        {
            return AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);
        }
        else if (reader is IoStoreReader && isIndex)
        {
            return AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);
        }

        return bytes[beginOffset..(beginOffset + count)];
    }
}
