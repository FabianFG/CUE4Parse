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
        var decrypted = (byte[]) encrypted.Clone();
        decrypted.DecryptInPlace(key);
        return decrypted;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(this ArraySegment<byte> encrypted, FAesKey key)
    {
        if (encrypted.Array is null) throw new ArgumentException("ArraySegment has no backing array.", nameof(encrypted));
        var decrypted = encrypted.AsSpan().ToArray();
        decrypted.DecryptInPlace(key);
        return decrypted;
    }

    public static byte[] Decrypt(this byte[] encrypted, int beginOffset, int count, FAesKey key)
    {
        ValidateRange(encrypted, beginOffset, count);
        var decrypted = encrypted.AsSpan(beginOffset, count).ToArray();
        decrypted.DecryptInPlace(key);
        return decrypted;
    }

    /// <summary>
    /// Decrypts an arbitrary range within an AES-ECB encryption unit. The input buffer must start at the
    /// encryption unit's first block and contain every complete block surrounding the requested range.
    /// </summary>
    public static byte[] DecryptRange(this byte[] encrypted, int beginOffset, int count, FAesKey key)
    {
        ValidateRange(encrypted, beginOffset, count);
        if (count == 0)
            return [];

        var alignedBeginOffset = beginOffset & -ALIGN;
        var alignedEndOffset = ((long) beginOffset + count).Align(ALIGN);
        if (alignedEndOffset > encrypted.Length)
            throw new ArgumentException(
                "Partial AES-ECB decryption requires the input buffer to contain the complete surrounding blocks.",
                nameof(count));

        var decrypted = encrypted.AsSpan(alignedBeginOffset, (int) alignedEndOffset - alignedBeginOffset).ToArray();
        decrypted.DecryptInPlace(key);

        var offsetInDecrypted = beginOffset - alignedBeginOffset;
        return offsetInDecrypted == 0 && count == decrypted.Length
            ? decrypted
            : decrypted.AsSpan(offsetInDecrypted, count).ToArray();
    }

    /// <summary>
    /// Decrypts complete AES-ECB blocks in place.
    /// </summary>
    public static void DecryptInPlace(this byte[] encrypted, FAesKey key) =>
        encrypted.DecryptInPlace(0, encrypted.Length, key);

    /// <summary>
    /// Decrypts complete AES-ECB blocks in place within an array.
    /// </summary>
    public static void DecryptInPlace(this byte[] encrypted, int beginOffset, int count, FAesKey key)
    {
        ValidateRange(encrypted, beginOffset, count);
        if (count % ALIGN != 0)
            throw new ArgumentException($"AES-ECB input length must be a multiple of {ALIGN} bytes.", nameof(count));
        if (count == 0)
            return;

        using var decryptor = Provider.CreateDecryptor(key.Key, null);
        var written = decryptor.TransformBlock(encrypted, beginOffset, count, encrypted, beginOffset);
        if (written != count)
            throw new CryptographicException($"AES-ECB decrypted {written} of {count} bytes.");
    }

    private static void ValidateRange(byte[] input, int beginOffset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (beginOffset > input.Length - count)
            throw new ArgumentException("The requested range exceeds the input buffer.", nameof(count));
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
