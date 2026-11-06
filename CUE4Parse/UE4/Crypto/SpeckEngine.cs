using System.Numerics;
using CommunityToolkit.HighPerformance;

namespace CUE4Parse.UE4.Crypto;

public static class SpeckEngine
{
    private const int Rounds = 34;

    public static void Decrypt(Span<byte> data, ReadOnlySpan<byte> key)
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
