using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.RocoKingdomWorld.Encryption.Aes;

public static class RocoKingdomWorldAes
{
    public static byte[] RocoKingdomWorldDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var ciphertext = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, ciphertext, 0, count);

        for (var i = 0; i < ciphertext.Length; i++)
        {
            ciphertext[i] = PermuteCiphertextByte(ciphertext[i]);
        }

        var gameKey = new FAesKey(MutateGameKey(reader.AesKey.Key), true);
        return ciphertext.Decrypt(gameKey);
    }

    private static byte PermuteCiphertextByte(byte value)
    {
        var stage1 = (byte) (((value & 0xD5) << 1) | ((value >> 1) & 0x55));
        var stage2 = (byte) (((stage1 & 0xF3) << 2) | ((stage1 >> 2) & 0x33));
        return (byte) ((stage2 >> 4) | (stage2 << 4));
    }

    private static byte[] MutateGameKey(byte[] input)
    {
        if (input.Length == 0)
            return Array.Empty<byte>();

        var output = new byte[input.Length];
        for (var i = 0; i < input.Length - 1; i++)
        {
            output[i] = input[input.Length - i - 2];
        }

        output[^1] = input[^1];
        return output;
    }
}
