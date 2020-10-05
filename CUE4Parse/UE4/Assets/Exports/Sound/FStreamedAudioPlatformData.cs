using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public class FStreamedAudioPlatformData
    {
        public int NumChunks;
        public FName AudioFormat;
        public FStreamedAudioChunk[] Chunks;

        public FStreamedAudioPlatformData(FAssetArchive Ar)
        {
            NumChunks = Ar.Read<int>();
            AudioFormat = Ar.ReadFName();
            Chunks = Ar.ReadArray(NumChunks, () => new FStreamedAudioChunk(Ar));
        }
    }
}
