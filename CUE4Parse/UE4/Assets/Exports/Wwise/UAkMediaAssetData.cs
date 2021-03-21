using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise
{
    [JsonConverter(typeof(UAkMediaAssetDataConverter))]
    public class UAkMediaAssetData : UObject
    {
        public bool IsStreamed { get; private set; } = false;
        public bool UseDeviceMemory { get; private set; } = false;
        public FAkMediaDataChunk[] DataChunks { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            // UObject Properties
            IsStreamed = GetOrDefault<bool>(nameof(IsStreamed));
            UseDeviceMemory = GetOrDefault<bool>(nameof(UseDeviceMemory));

            DataChunks = Ar.ReadArray(Ar.Read<int>(), () => new FAkMediaDataChunk(Ar));
        }

        private byte[]? _sound;
        public byte[]? Sound
        {
            get
            {
                if (_sound != null) return _sound;
                
                var offset = 0;
                _sound = new byte[DataChunks.Sum(x => x.Data.Data.Length)];
                foreach (var dataChunk in DataChunks)
                {
                    Buffer.BlockCopy(dataChunk.Data.Data, 0, _sound, offset, dataChunk.Data.Data.Length);
                    offset += dataChunk.Data.Data.Length;
                }
                return _sound;
            }
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
            
            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }
            
            // export properties
            writer.WritePropertyName("Properties");
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
