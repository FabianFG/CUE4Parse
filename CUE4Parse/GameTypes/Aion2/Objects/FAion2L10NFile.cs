using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.Aion2.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Aion2.Objects;

[JsonConverter(typeof(FAion2L10NConverter))]
public class FAion2L10NFile
{
    public string Namespace = string.Empty;
    public Dictionary<string, string> Entries = [];

    public FAion2L10NFile(GameFile file, IFileProvider provider)
    {
        var data = file.SafeRead();
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length >= 0x18 && BitConverter.ToUInt32(data, 0) == 2)
        {
            Aion2DatFileAes.Initialize(provider);

            var decrypted = Aion2DatFileAes.DecryptL10N(data);
            using var l10nAr = new FByteArchive("Aion2L10N", decrypted, null);
            if (l10nAr.Read<int>() != 1)
                throw new ParserException("Invalid AION2 L10N table version");
            Namespace = l10nAr.ReadFString();
            Entries = l10nAr.ReadMap(l10nAr.ReadFString, l10nAr.ReadFString);

            return;
        }

        using var Ar = new FAion2DatFileArchive(data, provider.Versions);

        Namespace = Ar.ReadL10NString();
        Entries = Ar.ReadMap(Ar.ReadL10NString, Ar.ReadL10NString);
    }
}

public class FAion2L10NConverter : JsonConverter<FAion2L10NFile>
{
    public override FAion2L10NFile? ReadJson(JsonReader reader, Type objectType, FAion2L10NFile? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FAion2L10NFile? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Namespace));
        writer.WriteValue(value.Namespace);
        writer.WritePropertyName(nameof(value.Entries));
        serializer.Serialize(writer, value.Entries);

        writer.WriteEndObject();
    }
}
