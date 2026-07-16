using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine
{
    public class UTextBuffer : Assets.Exports.UObject
    {
        public int Pos;
        public int Top;
        public string Text;

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);

            Pos = Ar.Read<int>();
            Top = Ar.Read<int>();
            Text = Ar.ReadFString();
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("Pos");
            writer.WriteValue(Pos);

            writer.WritePropertyName("Top");
            writer.WriteValue(Top);

            writer.WritePropertyName("Text");
            writer.WriteValue(Text);
        }
    }
}
