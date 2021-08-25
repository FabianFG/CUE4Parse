using System.IO;

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

        public long Length => _memoryData.Length;
        public long Position => _memoryData.Position;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _memoryData.Dispose();
        }
    }
}