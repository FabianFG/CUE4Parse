using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class SoundStructureSettings
    {
        public readonly uint OutputBusId;
        public readonly uint ParentId;
        public readonly bool OverrideParentPlaybackPriority;
        public readonly bool OffsetPriorityAtMaxDistance;
        public readonly ushort SettingsCount;
        public readonly Setting<ESoundStructureSettingsType>[] Settings;
        public readonly byte Unknown;

        public SoundStructureSettings(FArchive Ar)
        {
            OutputBusId = Ar.Read<uint>();
            ParentId = Ar.Read<uint>();
            OverrideParentPlaybackPriority = Ar.Read<bool>();
            OffsetPriorityAtMaxDistance = Ar.Read<bool>();
            SettingsCount = Ar.Read<ushort>();
            Settings = new Setting<ESoundStructureSettingsType>[SettingsCount];
            var settingIds = Ar.ReadArray<ESoundStructureSettingsType>(SettingsCount);
            var settingValues = Ar.ReadArray<float>(SettingsCount);
            for (int index = 0; index < SettingsCount; index++)
            {
                Settings[index] = new Setting<ESoundStructureSettingsType>(settingIds[index], settingValues[index]);
            }
            Unknown = Ar.Read<byte>();
        }

        public void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("OutputBusId");
            writer.WriteValue(OutputBusId);

            writer.WritePropertyName("ParentId");
            writer.WriteValue(ParentId);

            writer.WritePropertyName("OverrideParentPlaybackPriority");
            writer.WriteValue(OverrideParentPlaybackPriority);

            writer.WritePropertyName("OffsetPriotityAtMaxDistance");
            writer.WriteValue(OffsetPriorityAtMaxDistance);

            writer.WritePropertyName("Settings");
            writer.WriteStartObject();
            writer.WritePropertyName("SettingsCount");
            writer.WriteValue(SettingsCount);
            if (SettingsCount != 0)
            {
                writer.WritePropertyName("Settings");
                writer.WriteStartObject();
                foreach (Setting<ESoundStructureSettingsType> setting in Settings)
                    setting.WriteJson(writer, serializer);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();

            writer.WriteEndObject();

            
        }
    }
}
