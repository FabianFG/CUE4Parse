using System.IO;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.FileProvider
{
    public class OsGameFile : GameFile
    {
        public readonly FileInfo ActualFile;
        public OsGameFile(DirectoryInfo baseDir, FileInfo info) : base(info.FullName.Substring(baseDir.FullName.Length + 1).Replace('\\', '/'),
            info.Length)
        {
            ActualFile = info;
        }


        public override byte[] Read() => File.ReadAllBytes(ActualFile.FullName);

        public override FArchive CreateReader() => new FByteArchive(Path, Read());
    }
}