using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public class OsGameFile : GameFile
    {
        public readonly FileInfo ActualFile;
        public readonly VersionContainer Versions;

        public OsGameFile(DirectoryInfo baseDir, FileInfo info, string mountPoint, VersionContainer versions)
            : base(mountPoint + info.FullName.Substring(baseDir.FullName.Length + 1).Replace('\\', '/'), info.Length)
        {
            ActualFile = info;
            Versions = versions;
        }
        
        public override bool IsEncrypted => false;
        public override CompressionMethod CompressionMethod => CompressionMethod.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read() => File.ReadAllBytes(ActualFile.FullName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read(), Versions);
    }
}