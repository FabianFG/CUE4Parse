using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FieldPathPropertyConverter))]
    public class FieldPathProperty : FPropertyTagType<FFieldPath>
    {
        public FieldPathProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FFieldPath(),
                _ => new FFieldPath(Ar)
            };
        }
    }
    
    public class FieldPathPropertyConverter : JsonConverter<FieldPathProperty>
    {
        public override void WriteJson(JsonWriter writer, FieldPathProperty value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override FieldPathProperty ReadJson(JsonReader reader, Type objectType, FieldPathProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}