using System;
using System.Text;
using CUE4Parse.Encryption.Aes;

namespace CUE4Parse.GameTypes.Snowbreak.Encryption.Lua;

// Credits to ByteFun
public class SnowbreakLua
{
    private const int ChunkSize = 16;
    private readonly static byte[] _key = Encoding.ASCII.GetBytes("E1671E910EB54736B49908575F08CAFB");

    public static byte[] DecryptLua(byte[] data, int requestedSize)
    {
        if (data.Length == 0)
            return [];

        var length = Math.Min(data.Length, requestedSize);
        var alignedLength = length - (length % ChunkSize);

        var result = new byte[length];
        if (alignedLength > 0)
        {
            var encryptedData = data[..alignedLength];
            var decrypted = encryptedData.Decrypt(new FAesKey(_key));

            Buffer.BlockCopy(decrypted, 0, result, 0, alignedLength);
        }

        // There's no padding so leftover bytes aren't encrypted, we append them as is
        var tailLength = length - alignedLength;
        if (tailLength > 0)
        {
            Buffer.BlockCopy(data, alignedLength, result, alignedLength, tailLength);
        }

        return result;
    }
}
