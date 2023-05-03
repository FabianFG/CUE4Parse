using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.VirtualFileCache
{
    public class FFileTable
    {
        public readonly EVFCFileVersion FileVersion;
        public readonly FBlockFile[] BlockFiles;
        public readonly Dictionary<FSHAHash, FDataReference> FileMap;
        public readonly int LastBlockFileId;

        public FFileTable(FByteArchive Ar)
        {
            FileVersion = Ar.Read<EVFCFileVersion>();
            BlockFiles = Ar.ReadArray(() => new FBlockFile(Ar));
            FileMap = Ar.ReadMap(() => (new FSHAHash(Ar), new FDataReference(Ar)));
            LastBlockFileId = Ar.Read<int>();
        }
    }
}
