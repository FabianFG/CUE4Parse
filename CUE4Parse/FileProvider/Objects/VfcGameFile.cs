using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileCache;

namespace CUE4Parse.FileProvider.Objects
{
    public class VfcGameFile : GameFile
    {
        public readonly FBlockFile[] BlockFiles;
        public readonly FRangeId[] Ranges;
        public readonly VersionContainer Versions;

        private readonly string _persistentDownloadDir;

        public VfcGameFile(FBlockFile[] blockFiles, FDataReference dataReference, string persistentDownloadDir, string path, VersionContainer versions) : base(path, dataReference.TotalSize)
        {
            BlockFiles = blockFiles;
            Ranges = dataReference.Ranges;
            Versions = versions;

            _persistentDownloadDir = persistentDownloadDir;
        }

        public override bool IsEncrypted => false;
        public override CompressionMethod CompressionMethod => CompressionMethod.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read()
        {
            var offset = 0;
            var data = new byte[Size];
            foreach (var r in Ranges)
            {
                var blockSize = BlockFiles.First(x => x.FileId == r.FileId).BlockSize;
                using var fs = new FileStream(System.IO.Path.Combine(_persistentDownloadDir, r.GetPersistentDownloadPath()), FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Seek(r.Range.StartIndex * blockSize, SeekOrigin.Begin);
                offset += fs.Read(data, offset, r.Range.NumBlocks * blockSize);
            }
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read(), Versions);
    }
}
