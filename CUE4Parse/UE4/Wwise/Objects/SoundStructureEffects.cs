using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class SoundStructureEffects
    {
        public readonly bool OverrideParentEffects;
        public readonly byte EffectCount;
        public readonly EBypassEffectsType? BypassEffects;
        public readonly EffectReference[]? EffectReferences;

        public SoundStructureEffects(FArchive Ar)
        {
            OverrideParentEffects = Ar.Read<bool>();
            EffectCount = Ar.Read<byte>();
            if (EffectCount != 0)
            {
                BypassEffects = Ar.Read<EBypassEffectsType>();
                EffectReferences = Ar.ReadArray(EffectCount, () => new EffectReference(Ar));
            }
        }

        public void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("OverrideParentEffects");
            writer.WriteValue(OverrideParentEffects);

            writer.WritePropertyName("EffectCount");
            writer.WriteValue(EffectCount);

            if (EffectCount != 0)
            {
                writer.WritePropertyName("BypassEffects");
                writer.WriteValue(BypassEffects);

                writer.WritePropertyName("EffectReferences");
                writer.WriteStartArray();
                foreach (EffectReference effect in EffectReferences)
                    effect.WriteJson(writer, serializer);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
