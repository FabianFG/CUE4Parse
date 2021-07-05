using System.IO;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse.UE4.Writers
{
    public class FArchiveWriter : BinaryWriter
    {
        private readonly MemoryStream _memoryData;

        public FArchiveWriter()
        {
            _memoryData = new MemoryStream {Position = 0};
            OutStream = _memoryData;
        }

        public byte[] GetBuffer() => _memoryData.ToArray();

        public void SerializeChunkHeader(VChunkHeader header, string name)
        {
            header.ChunkId = name;
            header.TypeFlag = 20100422;
            header.Serialize(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _memoryData.Dispose();
        }
    }
}