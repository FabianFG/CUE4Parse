using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(BoolPropertyConverter))]
    public class BoolProperty : FPropertyTagType<bool>
    {
        public BoolProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            switch (type)
            {
                case ReadType.NORMAL when !Ar.HasUnversionedProperties:
                    Value = tagData?.Bool == true;
                    break;
                case ReadType.NORMAL:
                case ReadType.MAP:
                case ReadType.ARRAY:
                    Value = Ar.ReadFlag();
                    break;
                case ReadType.ZERO:
                    Value = tagData?.Bool == true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    public class BoolPropertyConverter : JsonConverter<BoolProperty>
    {
        public override void WriteJson(JsonWriter writer, BoolProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        public override BoolProperty ReadJson(JsonReader reader, Type objectType, BoolProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}