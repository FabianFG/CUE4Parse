using System.Buffers;
using System.Buffers.Binary;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CUE4Parse.Utils;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.Encryption.Aes;

public static class Aes
{
    public const int ALIGN = 16;
    public const int BLOCK_SIZE = 16 * 8;
    private const int CounterPrefixSize = 12;
    private const int MaxCtrScratchSize = 1024 * 1024;

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

    /// <summary>
    /// Applies Unreal's AES-CTR stream cipher to a range of <paramref name="input"/>.
    /// </summary>
    /// <param name="initialBlockIndex">The AES block index at the encrypted range's offset.</param>
    /// <param name="initialBlockByteOffset">The byte offset within <paramref name="initialBlockIndex"/>.</param>
    public static byte[] CryptCtr(this byte[] input, int beginOffset, int count, FAesKey key,
        ReadOnlySpan<byte> initializationVector, uint initialBlockIndex = 0, int initialBlockByteOffset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (beginOffset > input.Length - count)
            throw new ArgumentException("The requested range exceeds the input buffer.", nameof(count));

        var result = beginOffset == 0 && count == input.Length
            ? input
            : input.AsSpan(beginOffset, count).ToArray();
        result.AsSpan().CryptCtrInPlace(key, initializationVector, initialBlockIndex, initialBlockByteOffset);
        return result;
    }

    /// <summary>
    /// Applies Unreal's AES-CTR stream cipher in place.
    /// </summary>
    /// <param name="initialBlockIndex">The AES block index at the encrypted data's offset.</param>
    /// <param name="initialBlockByteOffset">The byte offset within <paramref name="initialBlockIndex"/>.</param>
    public static void CryptCtrInPlace(this Span<byte> data, FAesKey key,
        ReadOnlySpan<byte> initializationVector, uint initialBlockIndex = 0, int initialBlockByteOffset = 0)
    {
        if (initializationVector.Length != CounterPrefixSize)
            throw new ArgumentException($"Unreal AES-CTR initialization vectors must be {CounterPrefixSize} bytes.", nameof(initializationVector));
        if ((uint) initialBlockByteOffset >= ALIGN)
            throw new ArgumentOutOfRangeException(nameof(initialBlockByteOffset), initialBlockByteOffset,
                $"The byte offset within an AES block must be between 0 and {ALIGN - 1}.");
        if (data.IsEmpty)
            return;

        var scratchSize = Math.Min(data.Length, MaxCtrScratchSize).Align(ALIGN);
        var counterBlocks = ArrayPool<byte>.Shared.Rent(scratchSize);
        try
        {
            using var encryptor = Provider.CreateEncryptor(key.Key, null);
            var processed = 0;

            if (initialBlockByteOffset != 0)
            {
                var counter = counterBlocks.AsSpan(0, ALIGN);
                FillCounterBlocks(counter, initializationVector, initialBlockIndex);
                _ = encryptor.TransformBlock(counterBlocks, 0, ALIGN, counterBlocks, 0);

                var firstBlockLength = Math.Min(ALIGN - initialBlockByteOffset, data.Length);
                var firstBlock = data[..firstBlockLength];
                TensorPrimitives.Xor(firstBlock,
                    counter.Slice(initialBlockByteOffset, firstBlockLength), firstBlock);
                processed = firstBlockLength;
                if (processed < data.Length)
                    initialBlockIndex = checked(initialBlockIndex + 1);
            }

            while (processed < data.Length)
            {
                var dataLength = Math.Min(MaxCtrScratchSize, data.Length - processed);
                var encryptedLength = dataLength.Align(ALIGN);
                var counters = counterBlocks.AsSpan(0, encryptedLength);
                FillCounterBlocks(counters, initializationVector, initialBlockIndex);
                _ = encryptor.TransformBlock(counterBlocks, 0, encryptedLength, counterBlocks, 0);

                var dataChunk = data.Slice(processed, dataLength);
                TensorPrimitives.Xor(dataChunk, counters[..dataLength], dataChunk);
                processed += dataLength;
                if (processed < data.Length)
                    initialBlockIndex = checked(initialBlockIndex + (uint) (encryptedLength / ALIGN));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(counterBlocks);
        }
    }

    private static void FillCounterBlocks(Span<byte> counters, ReadOnlySpan<byte> initializationVector,
        uint initialBlockIndex)
    {
        for (var offset = 0; offset < counters.Length; offset += ALIGN)
        {
            var counter = counters.Slice(offset, ALIGN);
            initializationVector.CopyTo(counter);
            BinaryPrimitives.WriteUInt32BigEndian(counter[CounterPrefixSize..],
                checked(initialBlockIndex + (uint) (offset / ALIGN)));
        }
    }

    static Aes()
    {
        Provider = AesProvider.Create();
        Provider.Mode = CipherMode.ECB;
        Provider.Padding = PaddingMode.None;
        Provider.BlockSize = BLOCK_SIZE;
    }
}
