using System.Buffers.Binary;
using System.Text;
using Blake3;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.GameTypes.Aion2.Objects;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.GameTypes.Aion2.Encryption.Aes;

public sealed partial class Aion2DatFileEncryption
{
    private const ulong HeaderXorConstant = 0xCD02190910CE83F6;
    private static readonly ulong _headerKeyRoot = Blake3Hash(HeaderXorConstant);

    public static Dictionary<ulong, FAesKey> AesKeys = [];
    private static readonly Lock _instanceLock = new();

    public static void Initialize(IFileProvider provider)
    {
        if (AesKeys.Count != 0)
            return;

        var keyManifest = provider.Files.Values.FirstOrDefault(x => x.Path.EndsWith("/key_manifest.dat", StringComparison.OrdinalIgnoreCase))
            ?? throw new ParserException("Unable to locate key_manifest.dat");

        try
        {
            var manifestFile = new FAion2KeyManifestFile(keyManifest);
            lock (_instanceLock)
            {
                AesKeys = manifestFile.AesKeys;
            }
        }
        catch
        {
            Log.Error("Failed to read key_manifest.dat");
        }
    }

    private static byte[] DeriveHeaderKey(ulong seed, EHeaderType type)
    {
        Span<byte> input = stackalloc byte[24];
        BinaryPrimitives.WriteUInt64LittleEndian(input, _headerKeyRoot);
        BinaryPrimitives.WriteUInt64LittleEndian(input[8..], seed);
        BinaryPrimitives.WriteInt32LittleEndian(input[16..], (int) type);
        input[20..].Clear();
        using var hasher = Hasher.New();
        hasher.Update(input);

        return hasher.Finalize().AsSpan().ToArray();
    }

    private static ulong Blake3Hash(string value)
    {
        using var hasher = Hasher.New();
        hasher.Update(Encoding.UTF8.GetBytes(value));
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }

    private static ulong Blake3Hash(ulong value)
    {
        Span<byte> input = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(input, value);
        using var hasher = Hasher.New();
        hasher.Update(input);
        return BinaryPrimitives.ReadUInt64LittleEndian(hasher.Finalize().AsSpan());
    }

    private enum EHeaderType
    {
        CompressedDataTable = 1,
        StreamDataTable = 2,
        Localization = 3
    }

    private enum EEncryptionType : int
    {
        CompressedAES = 2,
        XorAES = 3
    }
}
