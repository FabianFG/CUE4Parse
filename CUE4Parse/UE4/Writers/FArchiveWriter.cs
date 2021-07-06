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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _memoryData.Dispose();
        }
    }
}