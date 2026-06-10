using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.VirtualFileSystem;
using AesProvider = CUE4Parse.Encryption.Aes.Aes;

namespace CUE4Parse.GameTypes.DeltaForce.Encryption.Aes;

public static class DragonSwordAes
{
    public static byte[] DragonSwordDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % 16 != 0)
            throw new ArgumentException("count must be a multiple of 16");
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var output = AesProvider.Decrypt(bytes, beginOffset, count, reader.AesKey);

        if (isIndex && count >= 16)
        {
            var xorbyte = output[2];
            Span<byte> key = stackalloc byte[8];
            key.Fill(xorbyte);
            var xorKey = BitConverter.ToUInt64(key);
            var span = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, ulong>(ref output[0]), count >> 3);
            for (var i = 0; i < span.Length; i++)
            {
                span[i] ^= xorKey;
            }
        }

        return output;
    }
}
