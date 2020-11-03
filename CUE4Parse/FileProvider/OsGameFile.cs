using System;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider
{
    public class OsGameFile : GameFile
    {
        public readonly FileInfo ActualFile;
        public OsGameFile(DirectoryInfo baseDir, FileInfo info, UE4Version ver, EGame game) : base(info.FullName.Substring(baseDir.FullName.Length + 1).Replace('\\', '/'),
            info.Length, ver, game)
        {
            ActualFile = info;
        }
        
        public override bool IsEncrypted => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read() => File.ReadAllBytes(ActualFile.FullName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read());
    }
}