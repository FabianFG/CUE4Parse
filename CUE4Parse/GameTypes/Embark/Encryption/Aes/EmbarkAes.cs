using System.Numerics;
using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.Embark.Encryption.Aes;

// Reversed by https://github.com/HappyDOGE
// Used in: Arc Raiders; The Finals;
public static class EmbarkAes
{
    private const int Rounds = 34;

    public static byte[] EmbarkDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
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

        SpeckDecrypt(output, reader.AesKey.Key);

        return AesProvider.Decrypt(output, 0, count, reader.AesKey);
    }

    private static void SpeckDecrypt(Span<byte> data, ReadOnlySpan<byte> key)
    {
        Span<ulong> roundKeys = stackalloc ulong[Rounds];
        Span<ulong> l = stackalloc ulong[36];

        var keySpan = key.Cast<byte, ulong>();
        roundKeys[0] = keySpan[0];
        l[0] = keySpan[1];
        l[1] = keySpan[2];
        l[2] = keySpan[3];

        for (var i = 0; i < Rounds - 1; i++)
        {
            l[i + 3] = (roundKeys[i] + BitOperations.RotateRight(l[i], 8)) ^ (ulong) i;
            roundKeys[i + 1] = BitOperations.RotateLeft(roundKeys[i], 3) ^ l[i + 3];
        }

        var span = data.Cast<byte, ulong>();
        for (var offset = 0; offset < span.Length; offset += 2)
        {
            var right = span[offset];
            var left = span[offset + 1];
            for (var i = Rounds - 1; i >= 0; i--)
            {
                right = BitOperations.RotateRight(right ^ left, 3);
                left = BitOperations.RotateLeft((left ^ roundKeys[i]) - right, 8);
            }

            span[offset] = right;
            span[offset + 1] = left;
        }
    }
}
