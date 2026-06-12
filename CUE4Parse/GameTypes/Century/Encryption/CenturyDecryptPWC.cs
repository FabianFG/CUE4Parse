namespace CUE4Parse.GameTypes.Century.Encryption;

/// <summary>
/// Reversed by Spiritovod
/// </summary>
public static class CenturyDecryptPWC
{
    private static readonly byte[] xbox = [
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7, 6, 4, 5, 9, 5, 3, 8, 7, 5, 3,
        7, 4, 6, 2, 8, 3, 7, 2, 7, 5, 6, 1, 10, 3, 2, 4, 3, 5, 3, 3, 4, 4, 4, 3, 3, 6, 3, 2, 6, 4, 4, 5, 5, 8, 5, 4, 6, 7
    ];

    public static void CenturyDecrypt(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var size = input.Length;
        if (size <= 5) return;

        int count = 0;
        int tmp1 = size - 5;
        int tmp2 = size - 8;
        byte xorByte = input[tmp1];

        while (tmp2 >= 0 && count + 1 < xbox.Length)
        {
            input[tmp2] ^= xorByte;

            tmp1 = tmp2 - xbox[count];
            if (tmp1 <= 0) break;

            xorByte = (byte) (input[tmp1] ^ xorByte);
            tmp2 = tmp1 - xbox[count + 1];
            count += 2;
        }

        if (tmp1 <= 0 || tmp2 <= 0) return;

        input[tmp2] ^= xorByte;

        if (tmp2 < 10) return;

        xorByte = (byte) (input[tmp2 - 6] ^ xorByte);
        input[tmp2 - 10] ^= xorByte;

        if (tmp2 < 24) return;

        xorByte = (byte) (input[tmp2 - 15] ^ xorByte);
        input[tmp2 - 24] ^= xorByte;

        if (tmp2 < 32) return;

        xorByte = (byte) (input[tmp2 - 29] ^ xorByte);
        input[tmp2 - 32] ^= xorByte;

        if (tmp2 >= 47)
        {
            xorByte = (byte) (input[tmp2 - 40] ^ xorByte);
            input[tmp2 - 47] ^= xorByte;
        }
    }
}
