using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class UConst : UField
    {
        public string Value;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Value = Ar.ReadFString();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WritePropertyName("Value");
            writer.WriteValue(Value);
        }
    }
}
