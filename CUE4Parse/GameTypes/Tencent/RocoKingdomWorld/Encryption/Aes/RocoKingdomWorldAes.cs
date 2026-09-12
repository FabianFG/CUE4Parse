using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Aes;

public static class RocoKingdomWorldAes
{
    public static byte[] RocoKingdomWorldDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var ciphertext = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, ciphertext, 0, count);

        if (customData is FPakCustomEncryptionData { Entry: var entry })
            return FConfigurableCrypto.DecryptChunked(ciphertext, (byte) entry.CustomData, reader.AesKey, entry.Name);

        var strategyIndex = (reader as PakFileReader)?.Info.CustomEncryptionData[0] ?? 0;
        return FConfigurableCrypto.Decrypt(ciphertext, strategyIndex, reader.AesKey, reader.Name);
    }
}
