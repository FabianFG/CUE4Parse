using System.IO;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider.Objects
{
    public class StreamedGameFile : VersionedGameFile
    {
        private readonly Stream _baseStream;

        public StreamedGameFile(string path, Stream stream, VersionContainer versions) : base(path, stream.Length, versions)
        {
            _baseStream = stream;
        }

        public override bool IsEncrypted => false;
        public override CompressionMethod CompressionMethod => CompressionMethod.None;

        public override byte[] Read()
        {
            var data = new byte[Size];
            var _ = _baseStream.Read(data, 0, data.Length);
            return data;
        }
    }
}
