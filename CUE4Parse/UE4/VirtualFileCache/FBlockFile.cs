using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.VirtualFileCache
{
    public class FBlockFile
    {
        public readonly EVFCFileVersion FileVersion;
        public readonly int FileId;
        public readonly int BlockSize;
        public readonly int NumBlocks;

        public readonly FBlockRange[] FreeRanges;
        public readonly FBlockRange[] UsedRanges;

        public FBlockFile(FByteArchive Ar)
        {
            FileVersion = Ar.Read<EVFCFileVersion>();
            FileId = Ar.Read<int>();
            BlockSize = Ar.Read<int>();
            NumBlocks = Ar.Read<int>();

            FreeRanges = Ar.ReadArray<FBlockRange>();
            UsedRanges = Ar.ReadArray<FBlockRange>();
        }
    }
}
