using System;
using System.Text;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.Common
{
    public class VChunkHeader
    {
        public string ChunkId;
        public int TypeFlag;
        public int DataSize;
        public int DataCount;

        public void Serialize(FArchiveWriter ar)
        {
            var id = new byte[20];
            var chunk = Encoding.UTF8.GetBytes(ChunkId);
            Buffer.BlockCopy(chunk, 0, id, 0, chunk.Length);
            
            ar.Write(id);
            ar.Write(TypeFlag);
            ar.Write(DataSize);
            ar.Write(DataCount);
        }
    }
}