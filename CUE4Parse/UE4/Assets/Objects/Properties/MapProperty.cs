using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;
using System;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(MapPropertyConverter))]
    public class MapProperty : FPropertyTagType<UScriptMap>
    {
        public MapProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
        {
            if (type == ReadType.ZERO)
            {
                Value = new UScriptMap();
            }
            else
            {
                if (tagData == null)
                    throw new ParserException(Ar, "Can't load MapProperty without tag data");
                Value = new UScriptMap(Ar, tagData);
            }
        }
    }

    public class MapPropertyConverter : JsonConverter<MapProperty>
    {
        public override void WriteJson(JsonWriter writer, MapProperty value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override MapProperty ReadJson(JsonReader reader, Type objectType, MapProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}