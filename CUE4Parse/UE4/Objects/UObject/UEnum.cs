using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [JsonConverter(typeof(UEnumConverter))]
    public class UEnum : Assets.Exports.UObject
    {
        /** List of pairs of all enum names and values. */
        public (FName, long)[] Names;

        /** How the enum was originally defined. */
        public ECppForm CppForm;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Names = Ar.ReadArray(() => (Ar.ReadFName(), Ar.Read<long>()));
            CppForm = (ECppForm) Ar.Read<byte>();
        }

        public enum ECppForm
        {
            Regular,
            Namespaced,
            EnumClass
        }
    }

    public class UEnumConverter : JsonConverter<UEnum>
    {
        public override void WriteJson(JsonWriter writer, UEnum value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);

            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }

            // export properties
            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            {
                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }
            }
            writer.WriteEndObject();

            writer.WritePropertyName("Names");
            writer.WriteStartObject();
            {
                foreach (var (name, enumValue) in value.Names)
                {
                    writer.WritePropertyName(name.Text);
                    writer.WriteValue(enumValue);
                }
            }
            writer.WriteEndObject();

            writer.WritePropertyName("CppForm");
            serializer.Serialize(writer, value.CppForm.ToString());

            writer.WriteEndObject();
        }

        public override UEnum ReadJson(JsonReader reader, Type objectType, UEnum existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}