using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.INikki.Encryption.Aes;

public static class InfinityNikkiAes
{
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

        // 32 bytes is optimal
        Span<byte> xorKey = stackalloc byte[32];
        xorKey[0] = key.Key[^1]; xorKey[15] = key.Key[0];
        xorKey[16] = key.Key[^1]; xorKey[^1] = key.Key[0];
        TensorUtils.Xor(data, xorKey);

        return data;
    }
}
