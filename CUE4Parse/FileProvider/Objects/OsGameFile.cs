using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider.Objects
{
    public class OsGameFile : VersionedGameFile
    {
        public readonly FileInfo ActualFile;

        public OsGameFile(DirectoryInfo baseDir, FileInfo info, string mountPoint, VersionContainer versions)
            : base(mountPoint + info.FullName.Substring(baseDir.FullName.Length + 1).Replace('\\', '/'), info.Length, versions)
        {
            ActualFile = info;
        }

        public override bool IsEncrypted => false;
        public override CompressionMethod CompressionMethod => CompressionMethod.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read() => File.ReadAllBytes(ActualFile.FullName);
    }
}
