using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.Tencent.GangstarMirageCity.Encryption;

public static class GangstarMirageCityAes
{
    internal static readonly byte[] _xorKey =
    [
        0xC7, 0x3A, 0x8B, 0x5D, 0x1F, 0x9E, 0x4C, 0x2D,
        0xA6, 0xB3, 0xD8, 0xE1, 0x3F, 0x7C, 0x2B, 0x9A
    ];

    public static byte[] GangstarMirageCityDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (beginOffset > bytes.Length - count)
            throw new ArgumentException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16", nameof(count));
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var output = AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);
        if (isIndex)
        {
            TensorUtils.Xor(output, _xorKey);
        }

        return output;
    }
}
