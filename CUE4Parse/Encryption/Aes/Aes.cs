using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.Encryption.Aes;

public static class Aes
{
    public const int ALIGN = 16;
    public const int BLOCK_SIZE = 16 * 8;
    private const int CounterPrefixSize = 12;

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

    public static byte[] CryptCtr(this byte[] input, int beginOffset, int count, FAesKey key,
        ReadOnlySpan<byte> initializationVector, uint initialBlockIndex = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (beginOffset > input.Length - count)
            throw new ArgumentException("The requested range exceeds the input buffer.", nameof(count));
        if (initializationVector.Length != CounterPrefixSize)
            throw new ArgumentException($"Unreal AES-CTR initialization vectors must be {CounterPrefixSize} bytes.", nameof(initializationVector));
        if (count == 0)
            return [];

        var blockCount = checked((int) ((count + (long) ALIGN - 1) / ALIGN));
        var counterBlocks = new byte[checked(blockCount * ALIGN)];
        for (var block = 0; block < blockCount; block++)
        {
            var counter = counterBlocks.AsSpan(block * ALIGN, ALIGN);
            initializationVector.CopyTo(counter);
            var blockIndex = checked(initialBlockIndex + (uint) block);
            BinaryPrimitives.WriteUInt32BigEndian(counter[CounterPrefixSize..], blockIndex);
        }

        using var encryptor = Provider.CreateEncryptor(key.Key, null);
        var keyStream = encryptor.TransformFinalBlock(counterBlocks, 0, counterBlocks.Length);
        var result = new byte[count];
        for (var i = 0; i < count; i++)
            result[i] = (byte) (input[beginOffset + i] ^ keyStream[i]);
        return result;
    }

    static Aes()
    {
        Provider = AesProvider.Create();
        Provider.Mode = CipherMode.ECB;
        Provider.Padding = PaddingMode.None;
        Provider.BlockSize = BLOCK_SIZE;
    }
}
