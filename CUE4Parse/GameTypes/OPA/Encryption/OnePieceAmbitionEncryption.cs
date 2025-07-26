using System;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.OPA.Encryption.Aes;

public static class OnePieceAmbitionEncryption
{
    public static byte[] OnePieceAmbitionDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");

        if (reader is not PakFileReader || isIndex) return bytes;

        int paddedLength = 16 * ((count + 15) >> 4);
        var output = new byte[paddedLength];
        Buffer.BlockCopy(bytes, 0, output, 0, count);
        for (int i = 0; i < paddedLength / 16; i++)
        {
            ulong conv1 = BitConverter.ToUInt64(output, i * 16 + 8) ^ 0xD6D6D6D6D6D6D6D6;
            ulong conv2 = BitConverter.ToUInt64(output, i * 16) ^ conv1;
            Buffer.BlockCopy(BitConverter.GetBytes(conv1), 0, output, i * 16, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(conv2), 0, output, i * 16 + 8, 8);
        }

        return output;
    }
}
