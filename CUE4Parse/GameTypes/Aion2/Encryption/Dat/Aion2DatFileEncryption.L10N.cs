using System.Runtime.InteropServices;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.GameTypes.Aion2.Objects;
using CUE4Parse.UE4.Exceptions;
using GenericReader;
using K4os.Compression.LZ4;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed partial class Aion2DatFileEncryption
{
    public static byte[] DecryptL10N(byte[] dat, string filePath)
    {
        using var Ar = new GenericBufferReader(dat);
        if (Ar.Length < 0x14 || Ar.Read<int>() != 2)
            throw new ParserException("AION2 L10N container is too small");

        var locale = GetL10NLocale(filePath);
        var seed = Blake3Hash($"L10NString_{locale}");

        var header = new FL10NHeader(Ar, seed);
        if (header.EncryptionType != EEncryptionType.CompressedAES)
            throw new ParserException($"Unsupported AION2 L10N encryption type '{header.EncryptionType}'");
        if (!AesKeys.TryGetValue(seed, out var key))
            throw new ParserException("AION2 L10N encryption key not found in manifest");

        var encryptedMemory = Ar.ReadMemory(header.AlignedSize);
        if (!MemoryMarshal.TryGetArray(encryptedMemory, out ArraySegment<byte> encrypted))
            throw new ParserException("AION2 L10N payload is not backed by an array");

        var decrypted = encrypted.Decrypt(key);
        var output = new byte[header.RawSize];
        var written = LZ4Codec.Decode(decrypted.AsSpan(0x20, header.PackedSize), output);
        if (written != output.Length)
            throw new ParserException($"AION2 L10N LZ4 decode failed ({written}/{output.Length})");

        return output;
    }

    private static string GetL10NLocale(string filePath)
    {
        var parts = filePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return parts[^2];

        throw new ParserException($"Unable to derive AION2 L10N locale from '{filePath}'");
    }

    private readonly struct FL10NHeader
    {
        public readonly int PackedSize;
        public readonly EEncryptionType EncryptionType;
        public readonly int AlignedSize;
        public readonly int RawSize;

        public FL10NHeader(GenericBufferReader Ar, ulong seed)
        {
            var data = Ar.ReadArray<byte>(0x10);
            FAion2DatFileArchive.DecryptData(data, DeriveHeaderKey(seed, EHeaderType.Localization));

            using var headerAr = new GenericBufferReader(data);
            PackedSize = headerAr.Read<int>();
            EncryptionType = headerAr.Read<EEncryptionType>();
            AlignedSize = headerAr.Read<int>();
            RawSize = headerAr.Read<int>();
        }
    }
}
