using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public enum EStreamedAudioChunk : uint
    {
        IsCooked			 = 1 << 0,
        HasSeekOffset		 = 1 << 1,
        IsInlined			 = 1 << 2,
    }

    [JsonConverter(typeof(FStreamedAudioChunkConverter))]
    public class FStreamedAudioChunk
    {
        public readonly int DataSize;
        public readonly int AudioDataSize;
        public readonly uint SeekOffsetInAudioFrames;
        public readonly FByteBulkData BulkData;

        public FStreamedAudioChunk(FAssetArchive Ar)
        {
            var flags = Ar.Read<EStreamedAudioChunk>();

            BulkData = new FByteBulkData(Ar);
            DataSize = Ar.Read<int>();
            AudioDataSize = Ar.Read<int>();

            if (flags.HasFlag(EStreamedAudioChunk.HasSeekOffset))
            {
                SeekOffsetInAudioFrames = Ar.Read<uint>();
            }
        }
    }
}
