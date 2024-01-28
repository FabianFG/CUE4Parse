using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class EffectReference
    {
        public readonly byte EffectIndex;
        public readonly uint EffectId;
        public readonly ushort Unknown;

        public EffectReference(FArchive Ar)
        {
            EffectIndex = Ar.Read<byte>();
            EffectId = Ar.Read<uint>();
            Unknown = Ar.Read<ushort>();
        }

        public void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("EffectIndex");
            writer.WriteValue(EffectIndex);

            writer.WritePropertyName("EffectId");
            writer.WriteValue(EffectId);

            // writer.WritePropertyName("Unknown");
            // writer.WriteValue(Unknown);

            writer.WriteEndObject();
        }
    }
}
