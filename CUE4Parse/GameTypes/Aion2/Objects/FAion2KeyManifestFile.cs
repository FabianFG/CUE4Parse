using Blake3;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using GenericReader;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Aion2.Objects;

[JsonConverter(typeof(FAion2KeyManifestFileConverter))]
public class FAion2KeyManifestFile
{
    private static readonly byte[] _manifestHashKey =
    [
        0xeb, 0x0b, 0xc0, 0x97, 0x28, 0x47, 0x5a, 0xb5,
        0x19, 0x6a, 0xf8, 0xce, 0x78, 0x5d, 0x8a, 0x79,
        0xdf, 0x18, 0xd2, 0x14, 0x8f, 0x51, 0xcb, 0xef,
        0x8b, 0x39, 0xe4, 0x3b, 0x2a, 0x1d, 0x40, 0x56
    ];

    public Dictionary<ulong, FAesKey> AesKeys = [];
    private const int PayloadOffset = 8;

    public FAion2KeyManifestFile(GameFile file)
    {
        var data = file.SafeRead() ?? throw new ParserException("Unable to read key_manifest.dat");

        if (data.Length < 8 || (data.Length - 8) % 0x30 != 0)
            throw new ParserException("Invalid AION2 key_manifest.dat header");

        var count = (data.Length - PayloadOffset) / 0x30;

        using var hasher = Hasher.New();
        hasher.Update(_manifestHashKey);
        var manifestKey = new FAesKey(hasher.Finalize().AsSpan().ToArray());
        var decrypted = data.Decrypt(PayloadOffset, count * 0x30, manifestKey);
        using var decryptedAr = new GenericBufferReader(decrypted);
        AesKeys = new Dictionary<ulong, FAesKey>(count);
        for (var i = 0; i < count; i++)
        {
            var seed = decryptedAr.Read<ulong>();
            var key = decryptedAr.ReadArray<byte>(32);
            decryptedAr.Position += 8;
            AesKeys[seed] = new FAesKey(key);
        }
    }
}

public class FAion2KeyManifestFileConverter : JsonConverter<FAion2KeyManifestFile>
{
    public override FAion2KeyManifestFile? ReadJson(JsonReader reader, Type objectType, FAion2KeyManifestFile? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FAion2KeyManifestFile? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.AesKeys));
        serializer.Serialize(writer, value?.AesKeys);

        writer.WriteEndObject();
    }
}
