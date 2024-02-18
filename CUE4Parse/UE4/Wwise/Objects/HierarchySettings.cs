using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchySettings : AbstractHierarchy
    {
        public readonly ushort SettingsCount;
        public readonly Setting<EHierarchyParameterType>[] Settings;

        public HierarchySettings(FArchive Ar) : base(Ar)
        {
            SettingsCount = Ar.Read<ushort>();
            Settings = new Setting<EHierarchyParameterType>[SettingsCount];
            var settingIds = Ar.ReadArray<EHierarchyParameterType>(SettingsCount);
            var settingValues = Ar.ReadArray<float>(SettingsCount);
            for(int index = 0; index < SettingsCount; index++)
            {
                Settings[index] = new Setting<EHierarchyParameterType>(settingIds[index], settingValues[index]);
            }
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // writer.WritePropertyName("SettingsCount");
            // writer.WriteValue(SettingsCount);

            // if (SettingsCount != 0)
            {
                writer.WritePropertyName("Settings");
                writer.WriteStartObject();
                foreach (Setting<EHierarchyParameterType> setting in Settings)
                    setting.WriteJson(writer, serializer);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
    }
}
