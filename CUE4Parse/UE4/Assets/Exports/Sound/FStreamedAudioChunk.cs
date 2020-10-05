using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    public class FStreamedAudioChunk
    {
        public int DataSize;
        public int AudioDataSize;
        public FByteBulkData BulkData;

        public FStreamedAudioChunk(FAssetArchive Ar)
        {
            if (Ar.ReadBoolean()) // bCooked
            {
                BulkData = new FByteBulkData(Ar);
                DataSize = Ar.Read<int>();
                AudioDataSize = Ar.Read<int>();
            }
            else
            {
                Ar.Position -= sizeof(int);
                throw new ParserException(Ar, "StreamedAudioChunks must be cooked");
            }
        }
    }
}
