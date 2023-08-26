using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    [JsonConverter(typeof(FStreamedAudioPlatformDataConverter))]
    public class FStreamedAudioPlatformData
    {
        public readonly int NumChunks;
        public readonly FName AudioFormat;
        public readonly FStreamedAudioChunk[] Chunks;

        public FStreamedAudioPlatformData(FAssetArchive Ar)
        {
            NumChunks = Ar.Read<int>();
            AudioFormat = Ar.ReadFName();
            Chunks = Ar.ReadArray(NumChunks, () => new FStreamedAudioChunk(Ar));
        }
    }
}
