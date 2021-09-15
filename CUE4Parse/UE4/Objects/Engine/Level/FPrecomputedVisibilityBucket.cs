using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Level
{
    public readonly struct FPrecomputedVisibilityBucket : IUStruct
    {
        public readonly int CellDataSize;
        public readonly FPrecomputedVisibilityCell[] Cells;
        public readonly FCompressedVisibilityChunk[] CellDataChunks;
        
        public FPrecomputedVisibilityBucket(FAssetArchive Ar)
        {
            CellDataSize = Ar.Read<int>();
            Cells = Ar.ReadArray<FPrecomputedVisibilityCell>();
            CellDataChunks = Ar.ReadArray(() => new FCompressedVisibilityChunk(Ar));
        }
    }
}