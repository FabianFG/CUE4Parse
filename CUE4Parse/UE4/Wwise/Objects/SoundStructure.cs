using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class SoundStructure
    {
        public readonly SoundStructureEffects SoundStructureEffectsData;
        public readonly SoundStructureSettings SoundStructureSettingsData;
        public readonly SoundStructurePosition SoundStructurePositionData;

        public SoundStructure(FArchive Ar)
        {
            SoundStructureEffectsData = new SoundStructureEffects(Ar);
            SoundStructureSettingsData = new SoundStructureSettings(Ar);
            SoundStructurePositionData = new SoundStructurePosition(Ar);
        }

        public void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Effects");
            SoundStructureEffectsData.WriteJson(writer, serializer);

            writer.WritePropertyName("Settings");
            SoundStructureSettingsData.WriteJson(writer, serializer);

            writer.WritePropertyName("Position");
            SoundStructurePositionData.WriteJson(writer, serializer);

            writer.WriteEndObject();
        }
    }
}
