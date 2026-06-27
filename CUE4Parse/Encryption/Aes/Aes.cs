using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.Encryption.Aes;

public static class Aes
{
    public const int ALIGN = 16;
    public const int BLOCK_SIZE = 16 * 8;

    private static readonly AesProvider Provider;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(this byte[] encrypted, FAesKey key)
    {
        return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, 0, encrypted.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(this ArraySegment<byte> encrypted, FAesKey key)
    {
        if (encrypted.Array is null) throw new ArgumentException("ArraySegment has no backing array.", nameof(encrypted));

        using var decryptor = Provider.CreateDecryptor(key.Key, null);
        return decryptor.TransformFinalBlock(encrypted.Array, encrypted.Offset, encrypted.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(this byte[] encrypted, int beginOffset, int count, FAesKey key)
    {
        return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, beginOffset, count);
    }

    static Aes()
    {
        Provider = AesProvider.Create();
        Provider.Mode = CipherMode.ECB;
        Provider.Padding = PaddingMode.None;
        Provider.BlockSize = BLOCK_SIZE;
    }
}
