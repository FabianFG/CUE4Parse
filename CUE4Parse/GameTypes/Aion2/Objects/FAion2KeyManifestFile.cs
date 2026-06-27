using Blake3;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Exceptions;
using GenericReader;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Aion2.Objects;

[JsonConverter(typeof(FAion2KeyManifestFileConverter))]
public class FAion2KeyManifestFile
{
    private static readonly byte[] KeyManifestMaterial =
    [
        0x9c, 0x9e, 0x42, 0x21, 0x0f, 0x2f, 0x5f, 0xbf,
        0x03, 0x0d, 0xa9, 0xab, 0xe9, 0xef, 0xa9, 0xab,
        0x6e, 0x73, 0x26, 0x35, 0x48, 0x5d, 0x7a, 0x6f,
        0x14, 0x09, 0xe4, 0x94, 0xcb, 0xfc, 0xbd, 0x4e
    ];

    public Dictionary<ulong, FAesKey> AesKeys = [];

    public FAion2KeyManifestFile(GameFile file, IFileProvider provider)
    {
        var data = file.SafeRead();
        if (data is null) throw new ParserException("Unable to read key_manifest.dat");
        using var Ar = new GenericBufferReader(data);
        var version = Ar.Read<int>();
        var count = Ar.Read<int>();
        var payloadSize = Ar.Read<int>();
        if (version != 2 || payloadSize != count * 0x30 || 12 + payloadSize > Ar.Length)
            throw new ParserException("Invalid AION2 key_manifest.dat header");

        using var hasher = Hasher.New();
        hasher.Update(KeyManifestMaterial);
        var manifestKey = new FAesKey(hasher.Finalize().AsSpan().ToArray());
        var decrypted = data.Decrypt(12, payloadSize, manifestKey);
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
        serializer.Serialize(writer, value.AesKeys);

        writer.WriteEndObject();
    }
}
