using System.Security.Cryptography;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.GameTypes.MisaOnline.Encryption.Aes;

public static class MisaOnlineAes
{
    private sealed class FArchiveKeyMaterial(byte[] rawKey, byte[] derived)
    {
        public byte[] RawKey { get; } = rawKey;
        public FAesKey Key { get; } = new(derived[..32]);
        public ReadOnlySpan<byte> OutputMask => derived.AsSpan(32, 16);
        public ReadOnlySpan<byte> InputMask => derived.AsSpan(48);
    }

    private static volatile FArchiveKeyMaterial? _cachedKeyMaterial;

    private static ReadOnlySpan<byte> KeyMask =>
    [
        0x6B, 0x14, 0x97, 0xF0, 0x2C, 0x27, 0xE3, 0x62, 0xAA, 0x64, 0xF5, 0xEC, 0x32, 0x75, 0x9A, 0xE9,
        0xF2, 0x93, 0x06, 0x51, 0x3A, 0x7B, 0x16, 0xCB, 0x67, 0x05, 0x3E, 0xED, 0xBF, 0xF3, 0xA5, 0x3D
    ];

    private static ReadOnlySpan<byte> FixedBlocks =>
    [
        0x4F, 0x54, 0xC5, 0x85, 0x9C, 0x17, 0x3C, 0xAD, 0x97, 0x62, 0xCF, 0x9D, 0x47, 0xF0, 0x14, 0x36,
        0xCB, 0x69, 0x26, 0x34, 0x7D, 0xD7, 0xC4, 0x8A, 0x9E, 0x8E, 0x1A, 0x99, 0xC7, 0x21, 0xA0, 0x51,
        0x2D, 0xFA, 0x79, 0x41, 0xA3, 0x5D, 0xBB, 0x56, 0x37, 0xF1, 0xBB, 0x22, 0x5B, 0x5E, 0x6B, 0x47,
        0x64, 0x03, 0x7C, 0x51, 0x91, 0x09, 0x91, 0x3A, 0x27, 0xF5, 0xDE, 0xDA, 0x76, 0x9F, 0xCD, 0xCA
    ];

    public static byte[] MisaOnlineDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        var key = reader.AesKey ?? throw new InvalidAesKeyException("Misa Online archives require an AES key.");
        var data = bytes.AsSpan(beginOffset, count).ToArray();
        var keyMaterial = GetArchiveKeyMaterial(key);

        TensorUtils.Xor(data, keyMaterial.InputMask);
        data.DecryptInPlace(keyMaterial.Key);
        TensorUtils.Xor(data, keyMaterial.OutputMask);
        return data;
    }

    private static FArchiveKeyMaterial GetArchiveKeyMaterial(FAesKey key)
    {
        var cached = _cachedKeyMaterial;
        if (cached is not null && key.Key.AsSpan().SequenceEqual(cached.RawKey))
            return cached;

        // AES Key callback obfuscation step
        var rawKey = (byte[]) key.Key.Clone();
        var processedKey = new byte[KeyMask.Length];
        for (var i = 0; i < processedKey.Length; i++)
            processedKey[i] = (byte) (rawKey[(5 * i + 29) % processedKey.Length] ^ KeyMask[i]);

        // and since that wasn't enough they do it again later
        // both functions are obfuscated too
        using var aes = AesProvider.Create();
        aes.Key = processedKey;
        var material = new FArchiveKeyMaterial(rawKey, aes.EncryptEcb(FixedBlocks, PaddingMode.None));
        _cachedKeyMaterial = material;
        return material;
    }
}
