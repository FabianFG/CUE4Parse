using System.Security.Cryptography;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Algorithms;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;

public static class FConfigurableCrypto
{
    private readonly record struct FCryptoStrategy(byte KeyId, byte AlgorithmId)
    {
        private const byte FileBasedKeyId = 0x80;

        public bool IsFileBasedKey => KeyId == FileBasedKeyId;
        public bool IsDefaultAesKey => KeyId == 0;

        public static FCryptoStrategy[] CreateTable(int algorithmCount, int keyCount)
        {
            // Default AES + static keys + file-based key per algorithm
            var strategies = new FCryptoStrategy[1 + algorithmCount * keyCount];

            var index = 0;
            strategies[index++] = new(0, 0); // Default AES
            for (byte algorithmId = 0; algorithmId < algorithmCount; algorithmId++)
            {
                for (byte keyId = 1; keyId < keyCount; keyId++) // the last key is not used??
                {
                    strategies[index++] = new(keyId, algorithmId);
                }

                strategies[index++] = new(FileBasedKeyId, algorithmId);
            }

            return strategies;
        }
    }

    private static readonly IConfigurableCryptoAlgorithm[] _algorithms =
    [
        new FConfigurableCryptoAlgorithmAES(),
        new FConfigurableCryptoAlgorithmNotImplemented("SM4"),
        new FConfigurableCryptoAlgorithmMLE(),
        new FConfigurableCryptoAlgorithmNotImplemented("RC5"),
        new FConfigurableCryptoAlgorithmNotImplemented("XTEA"),
        new FConfigurableCryptoAlgorithmNotImplemented("Speck"),
        new FConfigurableCryptoAlgorithmNotImplemented("Salsa20"),
        new FConfigurableCryptoAlgorithmNotImplemented("Simon"),
        new FConfigurableCryptoAlgorithmNotImplemented("ChaCha20")
    ];

    private static readonly byte[][] _keyMaterial =
    [
        [
            0xDF, 0x1B, 0x06, 0x73, 0xB2, 0xF4, 0x1A, 0x73,
            0x5B, 0xC4, 0xCE, 0x06, 0x84, 0xDC, 0x6F, 0x15,
            0xAD, 0x4F, 0xC6, 0x84, 0xF6, 0x88, 0xB4, 0x74,
            0x54, 0x32, 0x19, 0x34, 0xEA, 0xB3, 0x7A, 0xFF
        ],
        [
            0x7C, 0xE3, 0x61, 0x9C, 0xC9, 0x04, 0xE1, 0xFE,
            0xE2, 0xEE, 0x03, 0x77, 0xBB, 0xDE, 0xC0, 0xE5,
            0xBC, 0xB7, 0x61, 0xE8, 0xFC, 0xCF, 0x8E, 0x81,
            0x62, 0x03, 0x21, 0x55, 0x4A, 0x96, 0x41, 0xF7
        ],
        [
            0xE0, 0x8E, 0x51, 0x6E, 0xB7, 0xA3, 0x88, 0x8E,
            0xAD, 0x93, 0x8C, 0xD6, 0xAA, 0x9C, 0x82, 0x8A,
            0x25, 0x85, 0x87, 0x7E, 0xAB, 0x46, 0xC9, 0x69,
            0x4A, 0x62, 0x5D, 0x1B, 0xA2, 0xFF, 0x05, 0x9F
        ],
        [
            0x60, 0xD2, 0xF9, 0x57, 0x8A, 0x03, 0x22, 0xED,
            0x62, 0x31, 0xED, 0xC8, 0xD7, 0x5D, 0x03, 0x2E,
            0x80, 0x54, 0x33, 0x9D, 0x37, 0x13, 0x2F, 0x6F,
            0x52, 0xBA, 0xAB, 0xA6, 0xFE, 0xEA, 0x11, 0x55
        ],
        [
            0x23, 0x68, 0xF2, 0x14, 0xED, 0xD0, 0xF1, 0x8B,
            0xC0, 0x10, 0x34, 0xCF, 0x99, 0x62, 0x0F, 0x5D,
            0x64, 0x0D, 0x60, 0x23, 0x48, 0x0A, 0x90, 0x87,
            0x19, 0xD4, 0x9F, 0x5E, 0xC5, 0xC4, 0x7C, 0xA3
        ]
    ];
    private static readonly FCryptoStrategy[] _strategies = FCryptoStrategy.CreateTable(_algorithms.Length, _keyMaterial.Length);
    private static ReadOnlySpan<byte> DerivationSeed => "NRC_CRYPTO_OBFUSCATION_SEED2026\x07"u8;

    private sealed class FConfigurableCryptoAlgorithmNotImplemented(string name) : IConfigurableCryptoAlgorithm
    {
        public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key)
        {
            throw new NotImplementedException($"Algorithm {name} not yet implemented");
        }
    }

    private static byte[] DeriveFileNameKey(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        var encoded = Encoding.Unicode.GetBytes(name);

        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        sha1.AppendData(encoded);
        sha1.AppendData(DerivationSeed[..16]);

        var shaDigest = sha1.GetCurrentHash();

        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        md5.AppendData(shaDigest);
        md5.AppendData(DerivationSeed[16..]);

        var xorKey = md5.GetCurrentHash();

        var outputKey = new byte[32];
        TensorUtils.Xor(xorKey.AsSpan()[..16], DerivationSeed[..16], outputKey.AsSpan(0, 16));

        for (int i = 0; i < 16; i++)
        {
            outputKey[16 + i] = (byte) (xorKey[i] ^ DerivationSeed[DerivationSeed.Length - i - 1]);
        }

        return outputKey;
    }

    private static byte[] DeriveKey(byte[] keyMaterial)
    {
        var outputKey = new byte[keyMaterial.Length];
        var outputKeySpan = outputKey.AsSpan();

        var processed = 0;
        byte round = 0;

        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        while (processed != outputKey.Length)
        {
            sha1.AppendData(keyMaterial);
            sha1.AppendData(DerivationSeed);
            sha1.AppendData(new ReadOnlySpan<byte>(ref round));

            var currentHash = sha1.GetHashAndReset();
            var currentSize = Math.Min(outputKey.Length - processed, currentHash.Length);
            currentHash.AsSpan(0, currentSize).CopyTo(outputKeySpan.Slice(processed, currentSize));

            round++;
            processed += currentSize;
        }

        return outputKey;
    }

    private static byte[] GetKeyForStrategy(in FCryptoStrategy strategy, FAesKey? masterKey, string? fileName)
    {
        return strategy switch
        {
            { IsDefaultAesKey: true } => masterKey?.Key ?? throw new ArgumentNullException(nameof(masterKey)),
            { IsFileBasedKey: true } => fileName is not null ? DeriveFileNameKey(fileName) : throw new ArgumentNullException(nameof(fileName)),
            _ => DeriveKey(_keyMaterial[strategy.KeyId - 1])
        };
    }

    private static (IConfigurableCryptoAlgorithm Algorithm, byte[] Key) ResolveCryptoConfig(byte strategyIndex, FAesKey? masterKey, string? fileName)
    {
        if (strategyIndex >= _strategies.Length)
            throw new ArgumentOutOfRangeException(nameof(strategyIndex), strategyIndex, "Invalid crypto strategy index");

        var strategy = _strategies[strategyIndex];
        return (_algorithms[strategy.AlgorithmId], GetKeyForStrategy(strategy, masterKey, fileName));
    }

    public static byte[] Decrypt(byte[] ciphertext, byte strategyIndex, FAesKey? masterKey, string? fileName)
    {
        var (algorithm, key) = ResolveCryptoConfig(strategyIndex, masterKey, fileName);
        return algorithm.Decrypt(ciphertext, key);
    }

    public static byte[] DecryptChunked(byte[] ciphertext, byte strategyIndex, FAesKey? masterKey, string? fileName)
    {
        var (algorithm, key) = ResolveCryptoConfig(strategyIndex, masterKey, fileName);
        return algorithm.DecryptChunked(ciphertext, key);
    }
}
