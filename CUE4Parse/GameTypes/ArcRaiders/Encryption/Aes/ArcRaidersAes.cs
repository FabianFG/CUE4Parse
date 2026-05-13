using System;
using System.Buffers.Binary;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.ArcRaiders.Encryption.Aes;

// Reversed by https://github.com/HappyDOGE
public static class ArcRaidersAes
{
    // 0x047A8AC14396604CE1BAB46366C0A7FDBE40F66264D4625E2E6D11FF17272D7F
    public static byte[] ArcRaidersDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
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

        Decrypt(output, output.Length, reader.AesKey.Key);

        return AesProvider.Decrypt(output, 0, count, reader.AesKey);
    }

    private static void Decrypt(byte[] data, int len, byte[] key)
    {
        ulong[] rk = new ulong[34];
        ulong[] l = new ulong[36];

        rk[0] = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(0, 8));
        l[0] = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(8, 8));
        l[1] = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(16, 8));
        l[2] = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(24, 8));

        for (ulong i = 0; i < 33; i++)
        {
            l[i + 3] = (rk[i] + Ror64(l[i], 8)) ^ i;
            rk[i + 1] = Rol64(rk[i], 3) ^ l[i + 3];
        }

        for (int offset = 0; offset < len; offset += 16)
        {
            if (offset + 16 > data.Length)
                break;

            ulong R = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset, 8));
            ulong L = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset + 8, 8));

            for (int i = 33; i >= 0; i--)
            {
                R = Ror64(R ^ L, 3);
                L = Rol64((L ^ rk[i]) - R, 8);
            }

            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset, 8), R);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset + 8, 8), L);
        }
    }

    private static ulong Rol64(ulong value, int shift)
    {
        return (value << shift) | (value >> (64 - shift));
    }

    private static ulong Ror64(ulong value, int shift)
    {
        return (value >> shift) | (value << (64 - shift));
    }
}
