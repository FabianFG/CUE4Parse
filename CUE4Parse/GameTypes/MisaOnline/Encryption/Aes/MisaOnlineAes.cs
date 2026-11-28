using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using AesProvider = System.Security.Cryptography.Aes;

namespace CUE4Parse.GameTypes.MisaOnline.Encryption.Aes;

public static class MisaOnlineAes
{
    private sealed class FArchiveKeyMaterial(byte[] rawKey, byte[] derived)
    {
        public byte[] RawKey { get; } = rawKey;
        public byte[] Derived { get; } = derived;
        public FAesKey Key { get; } = new(derived[..32]);
        public ReadOnlySpan<byte> OutputMask => derived.AsSpan(32, 16);
        public ReadOnlySpan<byte> InputMask => derived.AsSpan(48);
    }

    private sealed class FChunkKeyMaterial(FArchiveKeyMaterial archive, FIoChunkId chunkId, FArchiveKeyMaterial material)
    {
        public FArchiveKeyMaterial Archive { get; } = archive;
        public FIoChunkId ChunkId { get; } = chunkId;
        public FArchiveKeyMaterial Material { get; } = material;
    }

    private static volatile FArchiveKeyMaterial? _cachedKeyMaterial;
    [ThreadStatic] private static FChunkKeyMaterial? _cachedChunkKeyMaterial;

    private static ReadOnlySpan<byte> KeyMask =>
    [
        0x6B, 0x14, 0x97, 0xF0, 0x2C, 0x27, 0xE3, 0x62, 0xAA, 0x64, 0xF5, 0xEC, 0x32, 0x75, 0x9A, 0xE9,
        0xF2, 0x93, 0x06, 0x51, 0x3A, 0x7B, 0x16, 0xCB, 0x67, 0x05, 0x3E, 0xED, 0xBF, 0xF3, 0xA5, 0x3D
    ];

    // Fixed AES inputs observed in the game's Pak and per-chunk IoStore key-material functions.
    private static ReadOnlySpan<byte> FixedBlocks =>
    [
        0xAB, 0x87, 0xAF, 0x62, 0x66, 0xA6, 0xC7, 0x81, 0x30, 0x9C, 0x2A, 0x14, 0x9B, 0xA5, 0xAF, 0xA9,
        0xCD, 0xF0, 0x1F, 0x2C, 0x19, 0xC0, 0x1E, 0x81, 0x8C, 0x27, 0x33, 0x9E, 0x91, 0x99, 0x9B, 0x35,
        0x2F, 0x96, 0xB8, 0xFD, 0xBE, 0xCD, 0x46, 0xC4, 0xEA, 0x59, 0x7E, 0xCF, 0xCE, 0x8F, 0xA6, 0x3A,
        0xEF, 0xF8, 0xCE, 0xE0, 0xB4, 0x2B, 0x4D, 0x51, 0x0C, 0x0B, 0x63, 0x38, 0x75, 0x5D, 0x97, 0xF4
    ];

    private static ReadOnlySpan<byte> ChunkKeyBlock =>
    [
        0xEB, 0xE0, 0xDF, 0x36, 0x11, 0x35, 0x2F, 0x8A, 0x06, 0x13, 0xDD, 0xAC, 0xDC, 0x10, 0xA9, 0x15,
        0x3B, 0x20, 0xDC, 0x3C, 0xA6, 0xD8, 0xA8, 0x18, 0xB3, 0xC6, 0xA2, 0x6D, 0xB2, 0x5F, 0x78, 0x3A
    ];

    private static ReadOnlySpan<byte> ChunkBlocks =>
    [
        0x87, 0x3F, 0x0C, 0x33, 0xC5, 0x2C, 0x33, 0xC6, 0xED, 0x9D, 0xBA, 0xA2, 0xDD, 0xCC, 0x50, 0x7D,
        0x2D, 0xC6, 0xCA, 0x18, 0x40, 0x95, 0xF1, 0xAC, 0xFA, 0xB5, 0xAE, 0xD8, 0x01, 0x66, 0xD5, 0x36,
        0x72, 0xA0, 0x95, 0xC9, 0x12, 0x1C, 0x38, 0x11, 0xEB, 0x66, 0xF0, 0xFF, 0x24, 0x75, 0x55, 0xEF,
        0x49, 0xE0, 0xA7, 0x7B, 0xBC, 0xD9, 0x42, 0xF3, 0x59, 0x4D, 0xA3, 0xFD, 0x0E, 0x53, 0x6B, 0x93
    ];

    public static byte[] MisaOnlineDecrypt(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader, object? customData = null)
    {
        var key = reader.AesKey ?? throw new InvalidAesKeyException("Misa Online archives require an AES key.");
        var data = bytes.AsSpan(beginOffset, count).ToArray();
        var keyMaterial = reader is IoStoreReader && !isIndex
            ? GetChunkKeyMaterial(key, customData is FIoChunkId chunkId
                ? chunkId
                : throw new ArgumentException("Misa Online IoStore decryption requires a chunk ID.", nameof(customData)))
            : GetArchiveKeyMaterial(key);

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

        var rawKey = (byte[]) key.Key.Clone();
        var processedKey = new byte[KeyMask.Length];
        for (var i = 0; i < processedKey.Length; i++)
            processedKey[i] = (byte) (rawKey[(5 * i + 29) % processedKey.Length] ^ KeyMask[i]);

        using var aes = AesProvider.Create();
        aes.Key = processedKey;
        var material = new FArchiveKeyMaterial(rawKey, aes.EncryptEcb(FixedBlocks, PaddingMode.None));
        _cachedKeyMaterial = material;
        return material;
    }

    private static FArchiveKeyMaterial GetChunkKeyMaterial(FAesKey key, FIoChunkId chunkId)
    {
        var archive = GetArchiveKeyMaterial(key);
        var cached = _cachedChunkKeyMaterial;
        if (cached is not null && ReferenceEquals(cached.Archive, archive) && cached.ChunkId == chunkId)
            return cached.Material;

        using var aes = AesProvider.Create();
        aes.Key = archive.Key.Key;
        var chunkKey = aes.EncryptEcb(ChunkKeyBlock, PaddingMode.None);
        aes.Key = chunkKey;

        Span<byte> input = stackalloc byte[64];
        ChunkBlocks.CopyTo(input);
        var chunkIdBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in chunkId, 1));
        for (var i = 0; i < 4; i++)
            TensorUtils.Xor(input.Slice(i * 16, 12), chunkIdBytes);

        var derived = aes.EncryptEcb(input, PaddingMode.None);
        TensorUtils.Xor(derived.AsSpan(32), archive.Derived.AsSpan(32));
        var material = new FArchiveKeyMaterial(archive.RawKey, derived);
        _cachedChunkKeyMaterial = new FChunkKeyMaterial(archive, chunkId, material);
        return material;
    }
}
