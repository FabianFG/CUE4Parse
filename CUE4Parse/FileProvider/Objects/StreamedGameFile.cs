using System.IO;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider.Objects
{
    public class StreamedGameFile : VersionedGameFile
    {
        private readonly Stream _baseStream;
        private readonly long _position;

        public StreamedGameFile(string path, Stream stream, VersionContainer versions) : base(path, stream.Length, versions)
        {
            _baseStream = stream;
            _position = _baseStream.Position;
        }

        public override bool IsEncrypted => false;
        public override CompressionMethod CompressionMethod => CompressionMethod.None;

        public override byte[] Read()
        {
            var data = new byte[Size];
            var _ = _baseStream.Seek(_position, SeekOrigin.Begin);
            var bytesRead = _baseStream.Read(data, 0, data.Length);
            if (bytesRead != Size)
                throw new Ionic.Zip.BadReadException("Read operation mismatch: bytesRead ≠ Size");
            return data;
        }
    }
}
