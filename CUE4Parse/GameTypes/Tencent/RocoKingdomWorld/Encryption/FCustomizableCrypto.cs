using System.Security.Cryptography;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption.Algorithms;

namespace CUE4Parse.GameTypes.Tencent.RocoKingdomWorld.Encryption;

public static class FCustomizableCrypto
{
    private readonly record struct CryptoStrategy(byte KeyId, byte AlgorithmId)
    {
        private const byte FileBasedKeyId = 0x80;

        public bool IsFileBasedKey => KeyId == FileBasedKeyId;
        public bool IsDefaultAesKey => KeyId == 0;
    }

    private static readonly List<ICustomizableCryptoAlgorithm> _algorithms;
    private static readonly List<byte[]> _keyMaterial;
    private static readonly List<CryptoStrategy> _strategies;
    private static ReadOnlySpan<byte> DerivationSeed => "NRC_CRYPTO_OBFUSCATION_SEED2026\x07"u8;

    static FCustomizableCrypto()
    {
        _algorithms =
        [
            new FCustomizableCryptoAlgorithmAes(),
            new FCustomizableCryptoAlgorithmNotImplemented("SM4"),
            new FCustomizableCryptoAlgorithmMLE(),
            new FCustomizableCryptoAlgorithmNotImplemented("RC5"),
            new FCustomizableCryptoAlgorithmNotImplemented("XTEA"),
            new FCustomizableCryptoAlgorithmNotImplemented("Speck"),
            new FCustomizableCryptoAlgorithmNotImplemented("Salsa20"),
            new FCustomizableCryptoAlgorithmNotImplemented("Simon"),
            new FCustomizableCryptoAlgorithmNotImplemented("ChaCha20")
        ];

        _keyMaterial =
        [
            [],
            [
                0xdf, 0x1b, 0x06, 0x73, 0xb2, 0xf4, 0x1a, 0x73, 0x5b, 0xc4, 0xce, 0x06, 0x84, 0xdc, 0x6f, 0x15, 0xad,
                0x4f, 0xc6, 0x84, 0xf6, 0x88, 0xb4, 0x74, 0x54, 0x32, 0x19, 0x34, 0xea, 0xb3, 0x7a, 0xff
            ],
            [
                0x7c, 0xe3, 0x61, 0x9c, 0xc9, 0x04, 0xe1, 0xfe, 0xe2, 0xee, 0x03, 0x77, 0xbb, 0xde, 0xc0, 0xe5, 0xbc,
                0xb7, 0x61, 0xe8, 0xfc, 0xcf, 0x8e, 0x81, 0x62, 0x03, 0x21, 0x55, 0x4a, 0x96, 0x41, 0xf7
            ],
            [
                0xe0, 0x8e, 0x51, 0x6e, 0xb7, 0xa3, 0x88, 0x8e, 0xad, 0x93, 0x8c, 0xd6, 0xaa, 0x9c, 0x82, 0x8a, 0x25,
                0x85, 0x87, 0x7e, 0xab, 0x46, 0xc9, 0x69, 0x4a, 0x62, 0x5d, 0x1b, 0xa2, 0xff, 0x05, 0x9f
            ],
            [
                0x60, 0xd2, 0xf9, 0x57, 0x8a, 0x03, 0x22, 0xed, 0x62, 0x31, 0xed, 0xc8, 0xd7, 0x5d, 0x03, 0x2e, 0x80,
                0x54, 0x33, 0x9d, 0x37, 0x13, 0x2f, 0x6f, 0x52, 0xba, 0xab, 0xa6, 0xfe, 0xea, 0x11, 0x55
            ],
            [
                0x23, 0x68, 0xf2, 0x14, 0xed, 0xd0, 0xf1, 0x8b, 0xc0, 0x10, 0x34, 0xcf, 0x99, 0x62, 0x0f, 0x5d, 0x64,
                0x0d, 0x60, 0x23, 0x48, 0x0a, 0x90, 0x87, 0x19, 0xd4, 0x9f, 0x5e, 0xc5, 0xc4, 0x7c, 0xa3
            ]
        ];

        _strategies =
        [
            new CryptoStrategy(0, 0)
        ];

        for (byte i = 0; i < _algorithms.Count; i++)
        {
            for (byte j = 1; j < _keyMaterial.Count - 1; j++) // the last key is not used??
            {
                _strategies.Add(new CryptoStrategy(j, i));
            }

            _strategies.Add(new CryptoStrategy(0x80, i));
        }
    }

    private sealed class FCustomizableCryptoAlgorithmNotImplemented(string name) : ICustomizableCryptoAlgorithm
    {
        public int MaximumKeySize => 0;
        public byte[] Decrypt(byte[] ciphertext, ReadOnlyMemory<byte> key)
        {
            throw new NotImplementedException($"Algorithm {name} not yet implemented");
        }
    }

    private static byte[] DeriveFileNameKey(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLower();
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
        for (int i = 0; i < 16; i++)
        {
            outputKey[i] = (byte)(xorKey[i] ^ DerivationSeed[i]);
        }

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

    private static byte[] GetKeyForStrategy(in CryptoStrategy strategy, FAesKey? masterKey, string? fileName)
    {
        if (strategy.IsDefaultAesKey)
        {
            if (masterKey == null)
                throw new ArgumentNullException(nameof(masterKey));

            return masterKey.Key;
        }

        if (strategy.IsFileBasedKey)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            return DeriveFileNameKey(fileName);
        }

        var keyMaterial = _keyMaterial[strategy.KeyId];
        return DeriveKey(keyMaterial);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte strategyIndex, FAesKey? masterKey, string? fileName)
    {
        var strategy = _strategies[strategyIndex];
        var key = GetKeyForStrategy(strategy, masterKey, fileName);
        return _algorithms[strategy.AlgorithmId].Decrypt(ciphertext, key);
    }
}
