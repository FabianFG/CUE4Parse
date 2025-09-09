using System;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.PMA.Encryption.Aes;

public class PMAAes
{
    /// <summary>
    /// Reversed by Spiritovod
    /// </summary>
    public static byte[] PMADecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        var key = reader?.AesKey?.Key;
        if (key is null)
            throw new InvalidAesKeyException("Reading encrypted data requires a valid aes key");
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");

        var decrypted = new byte[count];

        uint v5 = BitConverter.ToUInt32(key, 8);
        uint v6 = BitConverter.ToUInt32(key, 12);
        uint v7 = BitConverter.ToUInt32(key, 4);
        uint v9 = BitConverter.ToUInt32(key, 0);
        int v8 = ((count - 1) >> 4) + 1;

        for (int i = 0; i < v8; i++)
        {
            int offset = beginOffset + i * 16;
            int resultOffset = i * 16;
            uint v10 = BitConverter.ToUInt32(bytes, offset + 4);
            uint v11 = BitConverter.ToUInt32(bytes, offset);

            v10 -= (v11 + 0x5CA01372) ^ (v5 + 16 * v11) ^ (v6 + (v11 >> 5));
            v11 -= (v10 + 0x5CA01372) ^ (v7 + (v10 >> 5)) ^ (v9 + 16 * v10);
            uint v12 = v10 - ((v11 - 0x51AFF647) ^ (v6 + (v11 >> 5)) ^ (v5 + 16 * v11));
            v11 -= (v12 - 0x51AFF647) ^ (v7 + (v12 >> 5)) ^ (v9 + 16 * v12);
            BitConverter.GetBytes(v11).CopyTo(decrypted, resultOffset);
            BitConverter.GetBytes(v12).CopyTo(decrypted, resultOffset + 4);

            uint v13 = BitConverter.ToUInt32(bytes, offset + 8);
            uint v14 = BitConverter.ToUInt32(bytes, offset + 12);

            v14 -= (v13 + 0x5CA01372) ^ (v5 + 16 * v13) ^ (v6 + (v13 >> 5));
            uint v15 = v13 - ((v14 + 0x5CA01372) ^ (v7 + (v14 >> 5)) ^ (v9 + 16 * v14));
            uint v16 = v14 - ((v15 - 0x51AFF647) ^ (v5 + 16 * v15) ^ (v6 + (v15 >> 5)));
            BitConverter.GetBytes(v16).CopyTo(decrypted, resultOffset + 12);
            uint result = v16 - 0x51AFF647;
            BitConverter.GetBytes(v15 - (result ^ (v9 + 16 * v16) ^ (v7 + (v16 >> 5)))).CopyTo(decrypted, resultOffset + 8);
        }

        return decrypted;
    }
}
