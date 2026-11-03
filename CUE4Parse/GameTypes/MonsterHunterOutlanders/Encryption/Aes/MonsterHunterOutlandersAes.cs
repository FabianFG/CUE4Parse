using System.Numerics.Tensors;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.MonsterHunterOutlanders.Encryption.Aes;

public static class MonsterHunterOutlandersAes
{
    private const int BlockSize = 16;

    private static ReadOnlySpan<byte> RoundKeysXor =>
    [
        0xA7, 0x31, 0xD9, 0x56, 0x02, 0xCE, 0x84, 0x6B,
        0xF0, 0x19, 0xAD, 0x43, 0x95, 0xE2, 0x78, 0xBC
    ];

    public static byte[] MonsterHunterOutlandersDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % BlockSize != 0)
            throw new ArgumentException($"count must be a multiple of " + BlockSize, nameof(count));

        var key = reader.AesKey ?? throw new NullReferenceException("reader.AesKey");

        var output = bytes.AsSpan(beginOffset, count).ToArray();
        if (output.Length == 0)
            return output;

        Span<byte> roundKeyFold = stackalloc byte[BlockSize];
        TensorUtils.Xor(key.Key.AsSpan(0, BlockSize), RoundKeysXor, roundKeyFold);
        TensorPrimitives.Add(roundKeyFold, key.Key.AsSpan(BlockSize, BlockSize), roundKeyFold);

        TensorUtils.Xor(output, roundKeyFold);

        AesProvider.DecryptInPlace(output, key);
        return output;
    }
}
