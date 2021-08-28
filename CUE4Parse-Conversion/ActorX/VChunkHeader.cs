using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.ActorX
{
    public class VChunkHeader
    {
        public string ChunkId;
        public int TypeFlag;
        public int DataSize;
        public int DataCount;

        public void Serialize(FArchiveWriter Ar)
        {
            Ar.Write(ChunkId, 20);
            Ar.Write(TypeFlag);
            Ar.Write(DataSize);
            Ar.Write(DataCount);
        }
    }
}