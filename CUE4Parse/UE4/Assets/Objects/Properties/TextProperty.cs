using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(TextPropertyConverter))]
    public class TextProperty : FPropertyTagType<FText>
    {
        public TextProperty(FAssetArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new FText(0, ETextHistoryType.None, new FTextHistory.None()),
                _ => new FText(Ar)
            };
        }
    }
    
    public class TextPropertyConverter : JsonConverter<TextProperty>
    {
        public override void WriteJson(JsonWriter writer, TextProperty value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.Value);
        }

        public override TextProperty ReadJson(JsonReader reader, Type objectType, TextProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}