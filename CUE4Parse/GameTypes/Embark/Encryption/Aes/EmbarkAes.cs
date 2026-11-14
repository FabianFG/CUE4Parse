using CUE4Parse.UE4.Crypto;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.Embark.Encryption.Aes;

// Reversed by https://github.com/HappyDOGE
// Used in: Arc Raiders; The Finals;
public static class EmbarkAes
{
    public static byte[] EmbarkDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        if (!isIndex)
            return AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);

        var output = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, output, 0, count);

        SpeckEngine.Decrypt(output, reader.AesKey.Key);

        return AesProvider.Decrypt(output, 0, count, reader.AesKey);
    }
}
