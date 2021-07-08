using System;
using System.Text;

namespace CUE4Parse_Conversion.Meshes.Common
{
    public class VChunkHeader
    {
        public string ChunkId;
        public int TypeFlag;
        public int DataSize;
        public int DataCount;

        public void Serialize(FCustomArchiveWriter Ar)
        {
            var id = new byte[20];
            var chunk = Encoding.UTF8.GetBytes(ChunkId);
            Buffer.BlockCopy(chunk, 0, id, 0, chunk.Length);
            
            Ar.Write(id);
            Ar.Write(TypeFlag);
            Ar.Write(DataSize);
            Ar.Write(DataCount);
        }
    }
}