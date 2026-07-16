using System.Security.Cryptography;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Aion2.Objects;
using CUE4Parse.UE4.Exceptions;
using GenericReader;
using K4os.Compression.LZ4;
using Serilog;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed class Aion2DatFileAes
{
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

    public static byte[] DecryptL10N(byte[] dat)
    {
        if (dat.Length < 0x18)
            throw new ParserException("AION2 L10N container is too small");
        using var Ar = new GenericBufferReader(dat);
        var version = Ar.Read<int>();
        var seed = Ar.Read<ulong>();
        var rawSize = Ar.Read<int>();
        var packedSize = Ar.Read<int>();
        var alignedSize = Ar.Read<int>();
        if (version != 2 || rawSize <= 0 || packedSize <= 0 || alignedSize < packedSize || 0x18 + alignedSize > dat.Length)
            throw new ParserException("Invalid AION2 L10N container header");

        if (!AesKeys.TryGetValue(seed, out var key))
            throw new ParserException("AION2 L10N content key not found in manifest");

        var decrypted = dat.Decrypt(0x18, alignedSize, key);
        var raw = new byte[rawSize];
        var written = LZ4Codec.Decode(decrypted.AsSpan(0x10, packedSize), raw);
        if (written != rawSize) throw new ParserException($"AION2 L10N LZ4 decode failed ({written}/{rawSize})");
        return raw;
    }

    public static byte[] DecryptDataTable(byte[] dat)
    {
        using var Ar = new GenericBufferReader(dat);

        if (dat.Length < 0x1C || Ar.Read<int>() != 13)
            throw new ParserException("Invalid AION2 data table container header");

        var version = Ar.Read<int>();
        if (!AesKeys.TryGetValue(Ar.Read<ulong>(), out var key))
            throw new ParserException("Invalid AION2 data table container header");

        if (version == 2)
        {
            var rawSize = Ar.Read<int>();
            var packedSize = Ar.Read<int>();
            var alignedSize = Ar.Read<int>();
            if (rawSize <= 0 || packedSize <= 0 || alignedSize < packedSize || 0x1C + alignedSize > dat.Length)
                throw new ParserException("Invalid AION2 compressed data table header");
            var decrypted = dat.Decrypt(0x1C, alignedSize, key);

            var raw = new byte[rawSize];
            var written = LZ4Codec.Decode(decrypted.AsSpan(0x10, packedSize), raw);
            if (written != rawSize)
                throw new ParserException($"AION2 data table LZ4 decode failed ({written}/{rawSize})");
            return raw;
        }

        var counter = new byte[16];
        dat.AsSpan(16, 8).CopyTo(counter);
        Ar.Position += 8;
        var outputLength = Ar.Read<int>();
        if (version != 3 || outputLength != dat.Length - 0x1C)
            throw new ParserException("Unsupported AION2 data table cipher");

        var output = new byte[outputLength];
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
        return output;
    }
}
