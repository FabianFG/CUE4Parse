using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Algorithms;

public class FCustomizableCryptoAlgorithmAes : ICustomizableCryptoAlgorithm
{
    public int KeySize => 32;

    private static byte PermuteCiphertextByte(byte value)
    {
        var stage1 = (byte) (((value & 0xD5) << 1) | ((value >> 1) & 0x55));
        var stage2 = (byte) (((stage1 & 0xF3) << 2) | ((stage1 >> 2) & 0x33));
        return (byte) ((stage2 >> 4) | (stage2 << 4));
    }

    private static byte[] MutateGameKey(ReadOnlySpan<byte> input)
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

    public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key)
    {
        for (var i = 0; i < ciphertext.Length; i++)
        {
            ciphertext[i] = PermuteCiphertextByte(ciphertext[i]);
        }

        var mutatedKey = MutateGameKey(key.Span);

        var gameKey = new FAesKey(mutatedKey, true);
        return ciphertext.Decrypt(gameKey);
    }
}
