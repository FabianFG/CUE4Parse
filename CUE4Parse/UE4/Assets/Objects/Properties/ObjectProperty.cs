using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(ObjectPropertyConverter))]
    public class ObjectProperty : FPropertyTagType<FPackageIndex>
    {
        public ObjectProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FPackageIndex(Ar, 0),
                _ => new FPackageIndex(Ar)
            };
        }
    }

    public class ObjectPropertyConverter : JsonConverter<ObjectProperty>
    {
        public override void WriteJson(JsonWriter writer, ObjectProperty value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override ObjectProperty ReadJson(JsonReader reader, Type objectType, ObjectProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}