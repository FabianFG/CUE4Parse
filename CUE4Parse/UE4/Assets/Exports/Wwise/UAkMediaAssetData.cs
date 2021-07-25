using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise
{
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

            DataChunks = Ar.ReadArray(() => new FAkMediaDataChunk(Ar));
        }

        protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            base.WriteJson(writer, serializer);

            writer.WritePropertyName("DataChunks");
            writer.WriteStartArray();
            {
                foreach (var dataChunk in DataChunks)
                {
                    serializer.Serialize(writer, dataChunk);
                }
            }
            writer.WriteEndArray();
        }
    }
}
