using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.VirtualFileCache;

public class FBlockFile(FByteArchive Ar)
{
    public readonly EVFCFileVersion FileVersion = Ar.Read<EVFCFileVersion>();
    public readonly int FileId = Ar.Read<int>();
    public readonly int BlockSize = Ar.Read<int>();
    public readonly int NumBlocks = Ar.Read<int>();

    public readonly FBlockRange[] FreeRanges = Ar.ReadArray<FBlockRange>();
    public readonly FBlockRange[] UsedRanges = Ar.ReadArray<FBlockRange>();
}
