using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.GameFramework
{
    [JsonConverter(typeof(FUniqueNetIdReplConverter))]
    public class FUniqueNetIdRepl : IUStruct
    {
        public readonly FUniqueNetId? UniqueNetId;

        public FUniqueNetIdRepl(FArchive Ar)
        {
            var size = Ar.Read<int>();
            if (size > 0)
            {
                var type = Ar.ReadFName();
                var contents = Ar.ReadString();
                UniqueNetId = new FUniqueNetId(type.Text, contents);
            }
            else
            {
                UniqueNetId = null;
            }
        }
    }

    public class FUniqueNetIdReplConverter : JsonConverter<FUniqueNetIdRepl>
    {
        public override void WriteJson(JsonWriter writer, FUniqueNetIdRepl value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.UniqueNetId != null ? value.UniqueNetId : "INVALID");
        }

        public override FUniqueNetIdRepl ReadJson(JsonReader reader, Type objectType, FUniqueNetIdRepl existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
