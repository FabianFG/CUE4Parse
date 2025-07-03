using System;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.GameTypes.MindsEye.Encryption.Aes;

public static class MindsEyeAes
{
    private const int AES_BLOCKBYTES = 16;

    private static readonly byte[] LookupTable = GenerateLookupTable();
    private static readonly object LockObj = new();
    private static FAesKey CachedKey;
    private static FAesKey OrigKey;

    private static void FlipEndian(byte[] arr)
    {
        for (int i = 0; i < arr.Length; i += 4)
        {
            byte a = arr[i + 0];
            byte b = arr[i + 1];
            byte c = arr[i + 2];
            byte d = arr[i + 3];
            arr[i + 0] = d;
            arr[i + 1] = c;
            arr[i + 2] = b;
            arr[i + 3] = a;
        }
    }

    public static byte[] MindsEyeDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader)
    {
        if (bytes.Length < beginOffset + count)
            throw new IndexOutOfRangeException("beginOffset + count is larger than the length of bytes");
        if (count % AES_BLOCKBYTES != 0)
            throw new ArgumentException($"count must be a multiple of " + AES_BLOCKBYTES);
        if (reader.AesKey == null)
            throw new NullReferenceException("reader.AesKey");

        var plaintext = bytes[beginOffset..(beginOffset + count)];
        FAesKey key;

        // Cache key so we don't have to create a new flipped key every invocation
        lock (LockObj)
        {
            if (OrigKey != reader.AesKey)
            {
                // Make key with swapped endianess
                var keyBytes = (byte[]) reader.AesKey.Key.Clone();
                FlipEndian(keyBytes);
                CachedKey = new FAesKey(keyBytes);
                OrigKey = reader.AesKey;
            }
            key = CachedKey;
        }

        // Swap data endianess, decrypt, and swap back
        FlipEndian(plaintext);
        plaintext = plaintext.Decrypt(key);
        FlipEndian(plaintext);

        Span<byte> span = plaintext.AsSpan();
        if (isIndex)
        {
            int seed = 0x23212002;
            for (var i = 0; i < count; i++)
            {
                byte n = span[i];

                byte a = (byte) (n | seed);
                byte b = (byte) (~n | seed);
                byte c = (byte) (n & seed);
                byte d = (byte) (~n & seed);
                byte e = (byte) (n ^ seed);

                byte f = (byte) (a + seed);
                byte g = (byte) (d * 2 + c);

                byte r = (byte) ((((~a + 1 + c) * 4 + e * 3 - b + d) * 2 + ((g - seed) * 2 - e) * 9) * ~b);
                r += (byte) ((((f - (d + c) * 2) * 2 - d * 3 + e) * 3 - 1) * e);
                r += (byte) ((g * 9 - a * 12 + 2) * 2 * d);
                r += (byte) ((a * 6 - d * 9 - 1) * 2 * seed);
                r += (byte) (((g - f) * 6 + 1) * 2 * c);

                span[i] = r;
                seed = ~(~r | seed) * 3 + ~(r ^ seed) + (r | seed) + (r & seed) + (~r & seed) - r * 2 + 1;
            }
        }
        else
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = LookupTable[span[i]];
            }
        }
        return plaintext;
    }

    private static byte[] GenerateLookupTable()
    {
        var result = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            byte n = (byte) i;

            byte a = (byte) (n & 0xcb);
            byte b = (byte) (n & 0x34);
            byte c = (byte) (n | 0xcb);
            byte d = (byte) (n | 0x34);
            byte e = (byte) (a * 0xfd);
            byte f = (byte) (b * 0xfd);

            result[i] = (byte) (((~b + d) * 2 + ~(~e | f) + (~e & f) + (e & f) + (c * 3)) * 2 - 0x59 + ~(e ^ f));
        }
        return result;
    }
}
