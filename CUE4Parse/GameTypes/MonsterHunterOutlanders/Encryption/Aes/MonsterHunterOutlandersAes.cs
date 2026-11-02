using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.MonsterHunterOutlanders.Encryption.Aes;

public static class MonsterHunterOutlandersAes
{
    private const int BlockSize = 16;

    private static ReadOnlySpan<byte> RoundKeyFoldConstant =>
    [
        0xA7, 0x31, 0xD9, 0x56, 0x02, 0xCE, 0x84, 0x6B,
        0xF0, 0x19, 0xAD, 0x43, 0x95, 0xE2, 0x78, 0xBC
    ];

    public static byte[] MonsterHunterOutlandersDecrypt(
        byte[] bytes,
        int beginOffset,
        int count,
        bool isIndex,
        IAesVfsReader reader,
        object? customData = null)
    {
        var key = reader.AesKey ?? throw new InvalidOperationException("Archive has no AES key");
        return Decrypt(bytes, beginOffset, count, key);
    }

    public static byte[] Decrypt(byte[] bytes, int beginOffset, int count, FAesKey key)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(beginOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (beginOffset > bytes.Length - count)
            throw new ArgumentException("The requested range exceeds the input buffer.", nameof(count));
        if ((count & (BlockSize - 1)) != 0)
            throw new ArgumentException($"Encrypted byte count must be a multiple of {BlockSize}.", nameof(count));

        var output = bytes.AsSpan(beginOffset, count).ToArray();
        if (output.Length == 0)
            return output;

        Span<byte> roundKeyFold = stackalloc byte[BlockSize];
        var constant = RoundKeyFoldConstant;
        for (var i = 0; i < BlockSize; i++)
            roundKeyFold[i] = unchecked((byte) ((key.Key[i] ^ constant[i]) + key.Key[i + BlockSize]));

        for (var offset = 0; offset < output.Length; offset += BlockSize)
        {
            for (var i = 0; i < BlockSize; i++)
                output[offset + i] ^= roundKeyFold[i];
        }

        AesProvider.DecryptInPlace(output, key);
        return output;
    }
}
