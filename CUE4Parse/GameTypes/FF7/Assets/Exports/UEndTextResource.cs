using System.Collections.Generic;
using CUE4Parse.GameTypes.FF7.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FF7.Assets.Exports
{
    public class UEndTextResource : UObject
    {
        public Dictionary<string, FEndTextResourceStrings>? Strings;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            var count = Ar.Read<int>();
            Strings = new Dictionary<string, FEndTextResourceStrings>();
            for (var i = 0; i < count; ++i)
            {
                var key = Ar.ReadFString();
                if (string.IsNullOrWhiteSpace(key) || key[0] != '$')
                    throw new ParserException(Ar, $"EndTextResource '{Ar.Name}' does not start with a magic symbol!");
                var resource = new FEndTextResourceStrings(Ar);
                Strings.Add(key, resource);
            }
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            if (Strings?.Count > 0)
            {
                writer.WritePropertyName("Strings");
                writer.WriteStartObject();
                foreach ((string key, FEndTextResourceStrings val) in Strings)
                {
                    if (val.Entries == null || val.Entries?.Count <= 0) continue;
                    writer.WritePropertyName(key);
                    serializer.Serialize(writer, val.Entries);
                }
                writer.WriteEndObject();
            }
        }
    }
}
