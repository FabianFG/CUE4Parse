using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.Common
{
    public class FCustomArchiveWriter : FArchiveWriter
    {
        public void SerializeChunkHeader(VChunkHeader header, string name)
        {
            header.ChunkId = name;
            header.Serialize(this);
        }
    }
}