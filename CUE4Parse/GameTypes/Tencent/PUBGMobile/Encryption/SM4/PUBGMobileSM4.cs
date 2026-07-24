using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.UE4.Exceptions;
using Org.BouncyCastle.Utilities;

namespace CUE4Parse.GameTypes.Tencent.PUBGMobile.Encryption.SM4;

public enum EPUBGMobileEncryptionMethod
{
    ChainedXor = 16,
    SM4 = 17,
    LiteSaltSM4 = 18, // PUBG Lite
    // SM4_19 = 19,
    SaltSM4Min = 31,
    SaltSM4Max = 222
}

// sub_8887A48, sub_8887E2C
public static class PUBGMobileSM4
{
    private const int BLOCK_SIZE = 16;
    private static readonly byte[] _sm4KeyGlobal = SHA1.HashData(Encoding.ASCII.GetBytes("48ea7d82db9995f2")); // sub_A149F18, there's also "c44d833f54ba436f" for 0x13, haven't seen it used though
    private const string SM4KeyLite = "Q0hVTKey$nq*3ZFlQCiA";

    // Constructed at sub_A18BE0C to qword_F1B9528
    private static readonly string[] _pathKeySalts =
    [
        "xG2qW5lP7lV2iN5fN5pG", "xT1cJ6dL5wC0kK1rB4dK", "qC4jS5bZ6fL5xE6nD4zA",
        "gD4jQ2aL3bS3lC3xT0iW", "xU1yQ8wE9zY3gZ3bT5aE", "uQ3cO2dX7xY4xU7gH7iS",
        "gW1fR0jK6wQ4oN0oK1kZ", "aJ4pV7iZ7pU4wP2aC2cZ", "cX6jT3cM2oT3vK0kJ1qN",
        "iT2vS0cS6yT6cZ1sE1lO", "hM1pH9iY8wM9hT4lN5uJ", "kG6bC8jK0fL0dE4sH4mL",
        "dB6lB3vE0eZ8wM8rI0aC", "tP7sP7nI9rA2vQ4cV5yQ", "aT0cL1yN4pT3sZ7eM2vY",
        "uV6fU8fC9zN3mP5dH8mN", "rT6aQ6oZ1yM0gO5tO1aN", "jU5bH7lQ0fM9hK2kI0oF",
        "iQ0eM0mJ7uT0kV6kL5zY"
    ];

    public static byte[] Decrypt(byte[] bytes, int beginOffset, int count, string path, EPUBGMobileEncryptionMethod encryptionMethod, uint encryptionKeyId)
    {
        if (beginOffset > bytes.Length - count)
            throw new ArgumentException("beginOffset + count is larger than the length of bytes");
        if (encryptionMethod is EPUBGMobileEncryptionMethod.SM4 or EPUBGMobileEncryptionMethod.LiteSaltSM4
                or (>= EPUBGMobileEncryptionMethod.SaltSM4Min and <= EPUBGMobileEncryptionMethod.SaltSM4Max) && count % BLOCK_SIZE != 0)
            throw new ArgumentException($"{nameof(count)} must be a multiple of {BLOCK_SIZE}", nameof(count));

        var isDynamicallyEncrypted = (encryptionKeyId & 0xFF000000) == 0x01000000;
        var encryptionKeyIndex = encryptionKeyId & 0xFFFFFF;

        if (isDynamicallyEncrypted)
            throw new ParserException($"Dynamically encrypted assets aren't supported (EncryptionKeyId: 0x{encryptionKeyId:X})");

        var output = new byte[count];
        Buffer.BlockCopy(bytes, beginOffset, output, 0, count);
        switch (encryptionMethod)
        {
            case >= EPUBGMobileEncryptionMethod.SaltSM4Min and <= EPUBGMobileEncryptionMethod.SaltSM4Max:
            {
                var separator = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                var name = path.AsSpan(separator + 1);
                var extension = name.LastIndexOf('.');
                var cleanName = (extension >= 0 ? name[..extension] : name).ToString().ToLowerInvariant();
                var salt = _pathKeySalts[(encryptionMethod - EPUBGMobileEncryptionMethod.SaltSM4Min) % _pathKeySalts.Length];
                var keySource = string.Concat(cleanName, salt, encryptionMethod.ToString().ToLowerInvariant());

                Span<byte> key = stackalloc byte[20];
                SHA1.HashData(Encoding.ASCII.GetBytes(keySource), key);

                DecryptSm4(output, key[..BLOCK_SIZE]);
                break;
            }
            case EPUBGMobileEncryptionMethod.LiteSaltSM4:
            {
                var separator = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                var name = path.AsSpan(separator + 1);
                var extension = name.LastIndexOf('.');
                var cleanName = (extension >= 0 ? name[..extension] : name).ToString().ToLowerInvariant();
                var keySource = string.Concat(cleanName, SM4KeyLite);

                Span<byte> key = stackalloc byte[20];
                SHA1.HashData(Encoding.ASCII.GetBytes(keySource), key);
                DecryptSm4(output, key[..BLOCK_SIZE]);
                break;
            }
            case EPUBGMobileEncryptionMethod.SM4:
                DecryptSm4(output, _sm4KeyGlobal);
                break;
            case EPUBGMobileEncryptionMethod.ChainedXor:
                Decrypt16(output);
                break;
            default:
                throw new ParserException($"Unknown PUBG Mobile encryption method {encryptionMethod}");
        }

        return output;
    }

    private static void Decrypt16(Span<byte> output)
    {
        if (output.Length < 4)
            return;

        ReadOnlySpan<byte> key = [0xE5, 0x5B, 0x4E, 0xD1];
        for (var i = 0; i < 4; i++)
            output[i] ^= key[(output.Length + i) & 3];
        for (var i = 4; i < output.Length; i++)
            output[i] ^= output[i - 4];
    }

    private static void DecryptSm4(Span<byte> output, ReadOnlySpan<byte> key)
    {
        var engine = new PUBGMobileSM4Engine(key);
        for (var offset = 0; offset < output.Length; offset += BLOCK_SIZE)
            engine.ProcessBlock(output.Slice(offset, BLOCK_SIZE));
    }

    // Standard SM4 but with different constants
    private sealed class PUBGMobileSM4Engine
    {
        private readonly uint[] _rk;

        private static readonly uint[] _fk =
        [
            0x46970E9C, 0x4BC0685E, 0x59056186, 0xBCA2491E
        ];

        private static readonly uint[] _ck =
        [
            0x000EB92B, 0x3A0AE783, 0x9E3B5C67, 0xADDBDABF,
            0x7B7484CB, 0x49156C63, 0xC79AB5E7, 0x79EC9CFF,
            0x1725BEAB, 0x2FB89CA3, 0x24808AD7, 0xDDD28B1F,
            0x4740DA4B, 0xBBC3EA73, 0x247B30E7, 0x91BE385F,
            0x0401248B, 0x45FCD3A3, 0x530B4CE7, 0xC68DD35F,
            0xE3D16C2B, 0x4F698C13, 0x6B92C747, 0x769EFB1F,
            0x4C73BE9B, 0xC942B193, 0xAD80D827, 0x372FB33F,
            0x13CB6AAB, 0x2BDC0AA3, 0x17A4A247, 0xD5E96CAF
        ];

        private static readonly byte[] _sbox =
        [
            0x34, 0x66, 0x25, 0x74, 0x89, 0x78, 0xE4, 0xA9, 0x5A, 0x41, 0xBC, 0x7A, 0xD6, 0x16, 0x21, 0x23,
            0x4D, 0x61, 0xDA, 0x94, 0x9B, 0xDF, 0x13, 0x3C, 0x69, 0x3A, 0x31, 0x0A, 0x5F, 0xD7, 0x99, 0x95,
            0xF1, 0xAE, 0x72, 0x3D, 0x07, 0x60, 0x24, 0xB6, 0x98, 0xEE, 0xC4, 0xA2, 0x2D, 0x88, 0xDD, 0x8D,
            0x04, 0xEA, 0xBB, 0x11, 0xCA, 0x3E, 0x5D, 0xA1, 0xF6, 0x3F, 0xB0, 0x97, 0x80, 0x47, 0x2B, 0xA6,
            0xE6, 0xF7, 0xD9, 0xB1, 0x59, 0xC0, 0x7C, 0xBE, 0x54, 0x28, 0xB7, 0x7E, 0x4F, 0xF8, 0x43, 0x6E,
            0xA0, 0x50, 0x0E, 0xF5, 0x90, 0xB8, 0xFB, 0xA3, 0x7B, 0x62, 0x19, 0x46, 0x03, 0x2A, 0xB9, 0x8F,
            0x9F, 0x77, 0xB4, 0x5B, 0x83, 0x87, 0x08, 0xEB, 0xE2, 0x1E, 0x42, 0xF0, 0x0F, 0xE8, 0x71, 0x6A,
            0x75, 0xAD, 0x55, 0x1F, 0xB5, 0xAB, 0x33, 0xFA, 0x7F, 0x15, 0xBD, 0x85, 0xD8, 0x06, 0x68, 0xB3,
            0x52, 0x30, 0x48, 0x0B, 0x00, 0xED, 0xEF, 0xB2, 0x57, 0x8E, 0xE7, 0x6C, 0xD5, 0xE5, 0x2E, 0x53,
            0x82, 0x05, 0xF9, 0x81, 0xF4, 0x56, 0xBF, 0x8C, 0x4B, 0xE3, 0xDB, 0x4A, 0x91, 0x4C, 0x2C, 0xD3,
            0x40, 0x29, 0x4E, 0x20, 0x14, 0x36, 0x79, 0x09, 0x6F, 0xD1, 0x37, 0xE0, 0x39, 0x0C, 0x8A, 0x92,
            0x38, 0x12, 0x35, 0x6D, 0xE1, 0xFD, 0x93, 0x9A, 0x17, 0xD4, 0xC9, 0x9C, 0x6B, 0x84, 0x26, 0x9D,
            0xAF, 0x76, 0xC1, 0x9E, 0xD0, 0x96, 0xC5, 0xCB, 0xE9, 0x73, 0x49, 0xD2, 0xCD, 0x64, 0xC3, 0xC7,
            0x01, 0x7D, 0xF3, 0xAC, 0xFC, 0xDE, 0xA4, 0x44, 0x32, 0x1B, 0xC2, 0xBA, 0x1C, 0x02, 0xC6, 0x27,
            0x45, 0x8B, 0xF2, 0x18, 0xA7, 0x10, 0x51, 0x1D, 0xC8, 0xCF, 0x63, 0xFF, 0x2F, 0x0D, 0x58, 0xCE,
            0x65, 0xA5, 0xDC, 0x1A, 0x3B, 0x86, 0xFE, 0x22, 0x5C, 0xA8, 0x5E, 0x67, 0xAA, 0xEC, 0x70, 0xCC
        ];

        public PUBGMobileSM4Engine(ReadOnlySpan<byte> key)
        {
            if (key.Length < BLOCK_SIZE)
                throw new ArgumentException($"SM4 key must be at least {BLOCK_SIZE} bytes", nameof(key));

            _rk = new uint[32];
            ExpandKey(key);
        }

        // non-linear substitution tau.
        private static uint tau(uint A)
        {
            uint b0 = _sbox[A >> 24];
            uint b1 = _sbox[(A >> 16) & 0xFF];
            uint b2 = _sbox[(A >> 8) & 0xFF];
            uint b3 = _sbox[A & 0xFF];

            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        private static uint L_ap(uint B)
        {
            return B ^ Integers.RotateLeft(B, 13) ^ Integers.RotateLeft(B, 23);
        }

        private static uint T_ap(uint Z)
        {
            return L_ap(tau(Z));
        }

        // Key expansion
        private void ExpandKey(ReadOnlySpan<byte> key)
        {
            uint K0 = BinaryPrimitives.ReadUInt32BigEndian(key) ^ _fk[0];
            uint K1 = BinaryPrimitives.ReadUInt32BigEndian(key[4..]) ^ _fk[1];
            uint K2 = BinaryPrimitives.ReadUInt32BigEndian(key[8..]) ^ _fk[2];
            uint K3 = BinaryPrimitives.ReadUInt32BigEndian(key[12..]) ^ _fk[3];

            _rk[31] = K0 ^ T_ap(K1     ^ K2     ^ K3     ^ _ck[0]);
            _rk[30] = K1 ^ T_ap(K2     ^ K3     ^ _rk[31] ^ _ck[1]);
            _rk[29] = K2 ^ T_ap(K3     ^ _rk[31] ^ _rk[30] ^ _ck[2]);
            _rk[28] = K3 ^ T_ap(_rk[31] ^ _rk[30] ^ _rk[29] ^ _ck[3]);
            for (int i = 27; i >= 0; --i)
            {
                _rk[i] = _rk[i + 4] ^ T_ap(_rk[i + 3] ^ _rk[i + 2] ^ _rk[i + 1] ^ _ck[31 - i]);
            }
        }

        // Linear substitution L
        private static uint L(uint B)
        {
            return B ^ Integers.RotateLeft(B, 2) ^ Integers.RotateLeft(B, 10) ^ Integers.RotateLeft(B, 18) ^ Integers.RotateLeft(B, 24);
        }

        // Mixer-substitution T
        private static uint T(uint Z)
        {
            return L(tau(Z));
        }

        public int ProcessBlock(Span<byte> data)
        {
            uint X0 = BinaryPrimitives.ReadUInt32BigEndian(data);
            uint X1 = BinaryPrimitives.ReadUInt32BigEndian(data[4..]);
            uint X2 = BinaryPrimitives.ReadUInt32BigEndian(data[8..]);
            uint X3 = BinaryPrimitives.ReadUInt32BigEndian(data[12..]);

            for (int i = 0; i < 32; i += 4)
            {
                X0 ^= T(X1 ^ X2 ^ X3 ^ _rk[i    ]);  // F0
                X1 ^= T(X2 ^ X3 ^ X0 ^ _rk[i + 1]);  // F1
                X2 ^= T(X3 ^ X0 ^ X1 ^ _rk[i + 2]);  // F2
                X3 ^= T(X0 ^ X1 ^ X2 ^ _rk[i + 3]);  // F3
            }

            BinaryPrimitives.WriteUInt32BigEndian(data[0..], X3);
            BinaryPrimitives.WriteUInt32BigEndian(data[4..], X2);
            BinaryPrimitives.WriteUInt32BigEndian(data[8..], X1);
            BinaryPrimitives.WriteUInt32BigEndian(data[12..], X0);

            return BLOCK_SIZE;
        }
    }
}
