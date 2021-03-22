using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    [JsonConverter(typeof(USoundWaveConverter))]
    public class USoundWave : USoundBase
    {
        public bool bCooked { get; private set; }
        public bool bStreaming { get; private set; } = true;
        public FFormatContainer? CompressedFormatData { get; private set; }
        public FByteBulkData? RawData { get; private set; }
        public FGuid CompressedDataGuid { get; private set; }
        public FStreamedAudioPlatformData? RunningPlatformData { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            bStreaming = Ar.Game >= EGame.GAME_UE4_25;
            if (TryGetValue(out bool s, nameof(bStreaming))) // will return false if not found
                bStreaming = s;
            else if (TryGetValue(out FName loadingBehavior, "LoadingBehavior"))
                bStreaming = !loadingBehavior.IsNone && loadingBehavior.Text != "ESoundWaveLoadingBehavior::ForceInline";

            bCooked = Ar.ReadBoolean();
            if (!bStreaming)
            {
                if (bCooked)
                {
                    CompressedFormatData = new FFormatContainer(Ar);
                }
                else
                {
                    RawData = new FByteBulkData(Ar);
                }
                CompressedDataGuid = Ar.Read<FGuid>();
            }
            else
            {
                CompressedDataGuid = Ar.Read<FGuid>();
                RunningPlatformData = new FStreamedAudioPlatformData(Ar);
            }
        }
    }
    
    public class USoundWaveConverter : JsonConverter<USoundWave>
    {
        public override void WriteJson(JsonWriter writer, USoundWave value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }
            
            // export properties
            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            {
                writer.WritePropertyName("CompressedFormatData");
                serializer.Serialize(writer, value.CompressedFormatData);
                
                writer.WritePropertyName("RawData");
                serializer.Serialize(writer, value.RawData);
                
                writer.WritePropertyName("CompressedDataGuid");
                serializer.Serialize(writer, value.CompressedDataGuid);
                
                writer.WritePropertyName("RunningPlatformData");
                serializer.Serialize(writer, value.RunningPlatformData);
                
                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }
            }
            writer.WriteEndObject();
            
            writer.WriteEndObject();
        }

        public override USoundWave ReadJson(JsonReader reader, Type objectType, USoundWave existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
