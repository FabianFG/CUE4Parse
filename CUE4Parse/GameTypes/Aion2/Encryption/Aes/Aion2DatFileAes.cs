using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Blake3;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Aion2.Objects;
using CUE4Parse.UE4.Exceptions;
using GenericReader;
using K4os.Compression.LZ4;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed class Aion2DatFileAes
{
    private const ulong HeaderXorConstant = 0xCD02190910CE83F6;

    public static Dictionary<ulong, FAesKey> AesKeys = [];
    private static readonly Lock _instanceLock = new();

    public static void Initialize(IFileProvider provider)
    {
        if (AesKeys.Count != 0) return;
        var keyManifest = provider?.Files.Values.FirstOrDefault(x =>
            x.Path.EndsWith("/key_manifest.dat", StringComparison.OrdinalIgnoreCase));

        if (keyManifest is null) throw new ParserException("Unable to locate key_manifest.dat");
        try
        {
            var manifestFile = new FAion2KeyManifestFile(keyManifest, provider);
            lock ( _instanceLock)
            {
                AesKeys = manifestFile.AesKeys;
            }
        }
        catch
        {
            Log.Error("Failed to read key_manifest.dat");
        }
    }

    public static byte[] DecryptL10N(byte[] dat, string filePath)
    {
        if (dat.Length < 0x14 || BitConverter.ToInt32(dat, 0) != 2)
            throw new ParserException("AION2 L10N container is too small");

        if (TryReadLegacyL10NHeader(dat, out var legacySeed, out var legacyRawSize,
                out var legacyPackedSize, out var legacyAlignedSize))
        {
            return DecryptAndDecompressL10N(dat, 0x18, 0x10, legacySeed, legacyRawSize,
                legacyPackedSize, legacyAlignedSize);
        }

        var locale = GetL10NLocale(filePath);
        var seed = HashL10NSeed(locale);
        var xorStream = HeaderXorStream(seed, 3);
        Span<byte> decodedHeader = stackalloc byte[16];
        for (var i = 0; i < decodedHeader.Length; i++)
            decodedHeader[i] = (byte) (dat[4 + i] ^ xorStream[i]);

        var physical0 = BinaryPrimitives.ReadInt32LittleEndian(decodedHeader);
        var physical1 = BinaryPrimitives.ReadInt32LittleEndian(decodedHeader[4..]);
        var physical2 = BinaryPrimitives.ReadInt32LittleEndian(decodedHeader[8..]);
        var physical3 = BinaryPrimitives.ReadInt32LittleEndian(decodedHeader[12..]);
        var version = physical1;
        var rawSize = physical3;
        var packedSize = physical0;
        var alignedSize = physical2;
        if (version != 2 || rawSize <= 0 || packedSize <= 0 ||
            alignedSize < packedSize + 0x20 || (alignedSize & 0x0F) != 0 ||
            0x14 + alignedSize != dat.Length)
            throw new ParserException("Invalid current AION2 L10N container header");

        return DecryptAndDecompressL10N(dat, 0x14, 0x20, seed, rawSize, packedSize, alignedSize);
    }

    private static byte[] DecryptAndDecompressL10N(byte[] dat, int headerSize, int cryptoPrefixSize,
        ulong seed, int rawSize, int packedSize, int alignedSize)
    {

        if (!AesKeys.TryGetValue(seed, out var key))
            throw new ParserException("AION2 L10N content key not found in manifest");

        var decrypted = dat.Decrypt(headerSize, alignedSize, key);
        var raw = new byte[rawSize];
        var written = LZ4Codec.Decode(decrypted.AsSpan(cryptoPrefixSize, packedSize), raw);
        if (written != rawSize) throw new ParserException($"AION2 L10N LZ4 decode failed ({written}/{rawSize})");
        return raw;
    }

    private static bool TryReadLegacyL10NHeader(byte[] dat, out ulong seed, out int rawSize,
        out int packedSize, out int alignedSize)
    {
        seed = 0;
        rawSize = packedSize = alignedSize = 0;
        if (dat.Length < 0x18) return false;
        seed = BinaryPrimitives.ReadUInt64LittleEndian(dat.AsSpan(4));
        rawSize = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(12));
        packedSize = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(16));
        alignedSize = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(20));
        return rawSize > 0 && packedSize > 0 && alignedSize >= packedSize + 0x10 &&
               (alignedSize & 0x0F) == 0 && 0x18 + alignedSize == dat.Length;
    }

    private static string GetL10NLocale(string filePath)
    {
        var parts = filePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2) return parts[^2];
        throw new ParserException($"Unable to derive AION2 L10N locale from '{filePath}'");
    }

    private static ulong HashL10NSeed(string locale)
    {
        using var hasher = Hasher.New();
        hasher.Update(Encoding.UTF8.GetBytes($"L10NString_{locale}"));
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }

    private static byte[] HeaderXorStream(ulong seed, int mode)
    {
        var rootInput = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(rootInput, HeaderXorConstant);
        using var rootHasher = Hasher.New();
        rootHasher.Update(rootInput);
        var root = BinaryPrimitives.ReadUInt64LittleEndian(rootHasher.Finalize().AsSpan());

        var input = new byte[24];
        BinaryPrimitives.WriteUInt64LittleEndian(input, root);
        BinaryPrimitives.WriteUInt64LittleEndian(input.AsSpan(8), seed);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(16), mode);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(20), 0);
        using var hasher = Hasher.New();
        hasher.Update(input);
        return hasher.Finalize().AsSpan().ToArray();
    }

    public static byte[] DecryptDataTable(byte[] dat, string filePath)
    {
        if (dat.Length < 0x0C || BinaryPrimitives.ReadInt32LittleEndian(dat) != 13)
            throw new ParserException("Invalid AION2 data table container header");

        if (TryDecryptLegacyDataTable(dat, out var legacy)) return legacy;

        var seed = HashTableSeed(filePath);
        if (!AesKeys.TryGetValue(seed, out var key))
            throw new ParserException($"AION2 data table content key not found for '{filePath}'");

        if (TryReadCurrentCompressedHeader(dat, seed, out var rawSize, out var packedSize,
                out var alignedSize))
        {
            var decrypted = dat.Decrypt(0x14, alignedSize, key);
            var raw = new byte[rawSize];
            var written = LZ4Codec.Decode(decrypted.AsSpan(0x20, packedSize), raw);
            if (written != rawSize)
                throw new ParserException($"AION2 data table LZ4 decode failed ({written}/{rawSize})");
            return raw;
        }

        if (!TryReadCurrentStreamHeader(dat, seed, out var outputLength))
            throw new ParserException("Invalid current AION2 data table container header");

        var nonceInput = new byte[key.Key.Length + 4];
        key.Key.CopyTo(nonceInput, 0);
        Encoding.ASCII.GetBytes("nonc").CopyTo(nonceInput, key.Key.Length);
        using var nonceHasher = Hasher.New();
        nonceHasher.Update(nonceInput);
        var nonce = BinaryPrimitives.ReadUInt64LittleEndian(nonceHasher.Finalize().AsSpan());

        var output = new byte[outputLength];
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var encryptor = aes.CreateEncryptor(key.Key, null);
        var counter = new byte[16];
        var stream = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(counter, nonce);
        for (var offset = 0; offset < output.Length; offset += 16)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(counter.AsSpan(8), (ulong) offset / 16);
            encryptor.TransformBlock(counter, 0, 16, stream, 0);
            var blockSize = Math.Min(16, output.Length - offset);
            for (var i = 0; i < blockSize; i++) output[offset + i] = (byte) (dat[0x0C + offset + i] ^ stream[i]);
        }
        return output;
    }

    private static bool TryDecryptLegacyDataTable(byte[] dat, out byte[] output)
    {
        output = [];
        if (dat.Length < 0x1C) return false;
        var version = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(4));
        if (version is not (2 or 3)) return false;
        var seed = BinaryPrimitives.ReadUInt64LittleEndian(dat.AsSpan(8));
        if (!AesKeys.TryGetValue(seed, out var key)) return false;

        if (version == 2)
        {
            var rawSize = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(16));
            var packedSize = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(20));
            var alignedSize = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(24));
            if (rawSize <= 0 || packedSize <= 0 || alignedSize < packedSize || 0x1C + alignedSize > dat.Length)
                return false;
            var decrypted = dat.Decrypt(0x1C, alignedSize, key);

            output = new byte[rawSize];
            var written = LZ4Codec.Decode(decrypted.AsSpan(0x10, packedSize), output);
            if (written != rawSize)
                throw new ParserException($"AION2 data table LZ4 decode failed ({written}/{rawSize})");
            return true;
        }

        var counter = new byte[16];
        dat.AsSpan(16, 8).CopyTo(counter);
        var outputLength = BinaryPrimitives.ReadInt32LittleEndian(dat.AsSpan(24));
        if (version != 3 || outputLength != dat.Length - 0x1C)
            return false;

        output = new byte[outputLength];
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var encryptor = aes.CreateEncryptor(key.Key, null);
        var stream = new byte[16];
        for (var offset = 0; offset < output.Length; offset += 16)
        {
            encryptor.TransformBlock(counter, 0, 16, stream, 0);
            var blockSize = Math.Min(16, output.Length - offset);
            for (var i = 0; i < blockSize; i++) output[offset + i] = (byte) (dat[0x1C + offset + i] ^ stream[i]);
            for (var i = 8; i < 16 && ++counter[i] == 0; i++) { }
        }
        return true;
    }

    private static bool TryReadCurrentCompressedHeader(byte[] dat, ulong seed, out int rawSize,
        out int packedSize, out int alignedSize)
    {
        rawSize = packedSize = alignedSize = 0;
        if (dat.Length < 0x14) return false;
        var stream = HeaderXorStream(seed, 1);
        Span<byte> decoded = stackalloc byte[16];
        for (var i = 0; i < decoded.Length; i++) decoded[i] = (byte) (dat[4 + i] ^ stream[i]);
        packedSize = BinaryPrimitives.ReadInt32LittleEndian(decoded);
        var version = BinaryPrimitives.ReadInt32LittleEndian(decoded[4..]);
        alignedSize = BinaryPrimitives.ReadInt32LittleEndian(decoded[8..]);
        rawSize = BinaryPrimitives.ReadInt32LittleEndian(decoded[12..]);
        return version == 2 && rawSize > 0 && packedSize > 0 && alignedSize >= packedSize + 0x20 &&
               (alignedSize & 0x0F) == 0 && 0x14 + alignedSize == dat.Length;
    }

    private static bool TryReadCurrentStreamHeader(byte[] dat, ulong seed, out int outputLength)
    {
        outputLength = 0;
        var stream = HeaderXorStream(seed, 2);
        Span<byte> decoded = stackalloc byte[8];
        for (var i = 0; i < decoded.Length; i++) decoded[i] = (byte) (dat[4 + i] ^ stream[i]);
        outputLength = BinaryPrimitives.ReadInt32LittleEndian(decoded);
        var version = BinaryPrimitives.ReadInt32LittleEndian(decoded[4..]);
        return version == 3 && outputLength > 0 && 0x0C + outputLength == dat.Length;
    }

    private static ulong HashTableSeed(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        var name = normalized[(normalized.LastIndexOf('/') + 1)..];
        var extension = name.LastIndexOf('.');
        if (extension >= 0) name = name[..extension];
        using var hasher = Hasher.New();
        hasher.Update(Encoding.UTF8.GetBytes(name));
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }
}
