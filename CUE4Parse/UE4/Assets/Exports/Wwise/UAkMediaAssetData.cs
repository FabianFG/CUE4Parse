using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise
{
    [JsonConverter(typeof(UAkMediaAssetDataConverter))]
    public class UAkMediaAssetData : UObject
    {
        public bool IsStreamed { get; private set; } = false;
        public bool UseDeviceMemory { get; private set; } = false;
        public FAkMediaDataChunk[] DataChunks { get; private set; }

        public UAkMediaAssetData() { }
        public UAkMediaAssetData(FObjectExport exportObject) : base(exportObject) { }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            // UObject Properties
            IsStreamed = GetOrDefault<bool>(nameof(IsStreamed));
            UseDeviceMemory = GetOrDefault<bool>(nameof(UseDeviceMemory));

            DataChunks = Ar.ReadArray(Ar.Read<int>(), () => new FAkMediaDataChunk(Ar));
        }
    }
    
    public class UAkMediaAssetDataConverter : JsonConverter<UAkMediaAssetData>
    {
        public override void WriteJson(JsonWriter writer, UAkMediaAssetData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            // export properties
            writer.WritePropertyName("Export");
            writer.WriteStartObject();
            {
                writer.WritePropertyName("DataChunks");
                writer.WriteStartArray();
                {
                    foreach (var dataChunk in value.DataChunks)
                    {
                        serializer.Serialize(writer, dataChunk);
                    }
                }
                writer.WriteEndArray();
                
                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }
            }
            writer.WriteEndObject();
            
            writer.WriteEndObject();
        }

        public override UAkMediaAssetData ReadJson(JsonReader reader, Type objectType, UAkMediaAssetData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
