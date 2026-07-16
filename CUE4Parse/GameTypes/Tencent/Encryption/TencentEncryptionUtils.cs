using System.Numerics;
using System.Security.Cryptography;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.GameTypes.Tencent.Encryption;

public class TencentEncryptionUtils
{
    public const int SHA1_SIZE = 20;
    public const int RSA_SIZE = 256;

    public static byte[] DecryptRsaOaep(ReadOnlySpan<byte> encrypted, byte[] modulus)
    {
        var value = BigInteger.ModPow(new BigInteger(encrypted, isUnsigned: true, isBigEndian: false), 65537, new BigInteger(modulus, isUnsigned: true, isBigEndian: false));
        var encoded = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        Array.Resize(ref encoded, (encoded.Length + 3) & ~3);

        return DecodeOaep(encoded);
    }

    public static byte[] DecodeOaep(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length < SHA1_SIZE * 2 + 2 || encoded[0] != 0)
            throw new ParserException("Invalid OAEP block");

        var seed = encoded.Slice(1, SHA1_SIZE).ToArray();
        var database = encoded[(1 + SHA1_SIZE)..].ToArray();
        XorRepeatedHash(database, seed);
        XorRepeatedHash(seed, database);

        var expectedLabelHash = SHA1.HashData(new byte[SHA1_SIZE]);
        if (!database.AsSpan(0, SHA1_SIZE).SequenceEqual(expectedLabelHash))
            throw new ParserException("Invalid OAEP label hash");

        var marker = database.AsSpan(SHA1_SIZE).IndexOf((byte) 1);
        if (marker < 0)
            throw new ParserException("Invalid OAEP padding");

        return database[(SHA1_SIZE + marker + 1)..];
    }

    private static void XorRepeatedHash(ReadOnlySpan<byte> source, Span<byte> target)
    {
        var hash = SHA1.HashData(source);
        for (var i = 0; i < target.Length; i++)
            target[i] ^= hash[i % hash.Length];
    }
}
