using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.FileProvider.Objects
{
    public abstract class VersionedGameFile : GameFile
    {
        public readonly VersionContainer Versions;

        public VersionedGameFile(string path, long size, VersionContainer versions) : base(path, size)
        {
            Versions = versions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader(FByteBulkDataHeader? header = null) => new FByteArchive(Path, Read(header), Versions);
    }
}
