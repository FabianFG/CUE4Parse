using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [JsonConverter(typeof(UStructConverter))]
    public class UStruct : Assets.Exports.UObject
    {
        public FPackageIndex SuperStruct;
        public FPackageIndex[] Children;
        public FField[] ChildProperties;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            SuperStruct = new FPackageIndex(Ar);
            if (FFrameworkObjectVersion.Get(Ar) < FFrameworkObjectVersion.Type.RemoveUField_Next)
            {
                throw new NotImplementedException();
            }
            else
            {
                Children = Ar.ReadArray(() => new FPackageIndex(Ar));
            }

            if (FCoreObjectVersion.Get(Ar) >= FCoreObjectVersion.Type.FProperties)
            {
                DeserializeProperties(Ar);
            }

            var bytecodeBufferSize = Ar.Read<int>();
            var serializedScriptSize = Ar.Read<int>();
            Ar.Position += serializedScriptSize; // should we read the bytecode some day?
        }

        protected void DeserializeProperties(FAssetArchive Ar)
        {
            ChildProperties = Ar.ReadArray(() =>
            {
                var propertyTypeName = Ar.ReadFName();
                var prop = FField.Construct(propertyTypeName);
                prop.Deserialize(Ar);
                return prop;
            });
        }
    }

    public class UStructConverter : JsonConverter<UStruct>
    {
        public override void WriteJson(JsonWriter writer, UStruct value, JsonSerializer serializer)
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

            writer.WritePropertyName("SuperStruct");
            serializer.Serialize(writer, value.SuperStruct);

            writer.WritePropertyName("Children");
            serializer.Serialize(writer, value.Children);

            writer.WritePropertyName("ChildProperties");
            serializer.Serialize(writer, value.ChildProperties);

            writer.WriteEndObject();
        }

        public override UStruct ReadJson(JsonReader reader, Type objectType, UStruct existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}