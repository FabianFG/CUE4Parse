using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.VirtualFileCache;

public class FFileTable(FByteArchive Ar)
{
    public readonly EVFCFileVersion FileVersion = Ar.Read<EVFCFileVersion>();
    public readonly FBlockFile[] BlockFiles = Ar.ReadArray(() => new FBlockFile(Ar));
    public readonly Dictionary<FSHAHash, FDataReference> FileMap = Ar.ReadMap(() => (new FSHAHash(Ar), new FDataReference(Ar)));
    public readonly int LastBlockFileId = Ar.Read<int>();
}
