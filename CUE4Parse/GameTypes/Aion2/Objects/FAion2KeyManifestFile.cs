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
    private static readonly byte[] OldKeyManifestMaterial =
    [
        0x9c, 0x9e, 0x42, 0x21, 0x0f, 0x2f, 0x5f, 0xbf,
        0x03, 0x0d, 0xa9, 0xab, 0xe9, 0xef, 0xa9, 0xab,
        0x6e, 0x73, 0x26, 0x35, 0x48, 0x5d, 0x7a, 0x6f,
        0x14, 0x09, 0xe4, 0x94, 0xcb, 0xfc, 0xbd, 0x4e
    ];
    private static readonly byte[] CurrentKeyManifestMaterial =
    [
        0xeb, 0x0b, 0xc0, 0x97, 0x28, 0x47, 0x5a, 0xb5,
        0x19, 0x6a, 0xf8, 0xce, 0x78, 0x5d, 0x8a, 0x79,
        0xdf, 0x18, 0xd2, 0x14, 0x8f, 0x51, 0xcb, 0xef,
        0x8b, 0x39, 0xe4, 0x3b, 0x2a, 0x1d, 0x40, 0x56
    ];

    public Dictionary<ulong, FAesKey> AesKeys = [];

    public FAion2KeyManifestFile(GameFile file, IFileProvider provider)
    {
        var data = file.SafeRead();
        if (data is null) throw new ParserException("Unable to read key_manifest.dat");

        int count;
        int payloadOffset;
        byte[] material;
        if (data.Length >= 12 && BitConverter.ToInt32(data, 0) == 2)
        {
            count = BitConverter.ToInt32(data, 4);
            var payloadSize = BitConverter.ToInt32(data, 8);
            if (count <= 0 || payloadSize != count * 0x30 || 12 + payloadSize != data.Length)
                throw new ParserException("Invalid legacy AION2 key_manifest.dat header");
            payloadOffset = 12;
            material = OldKeyManifestMaterial;
        }
        else if (data.Length >= 8 && (data.Length - 8) % 0x30 == 0)
        {
            payloadOffset = 8;
            count = (data.Length - payloadOffset) / 0x30;
            material = CurrentKeyManifestMaterial;
        }
        else
        {
            throw new ParserException("Invalid AION2 key_manifest.dat header");
        }

        using var hasher = Hasher.New();
        hasher.Update(material);
        var manifestKey = new FAesKey(hasher.Finalize().AsSpan().ToArray());
        var decrypted = data.Decrypt(payloadOffset, count * 0x30, manifestKey);
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
