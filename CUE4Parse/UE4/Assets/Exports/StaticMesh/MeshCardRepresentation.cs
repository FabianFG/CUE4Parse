using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.StaticMesh
{
    public struct FLumenCardBuildData
    {
        public FVector Center;
        public FVector Extent;

        // -X, +X, -Y, +Y, -Z, +Z
        public int Orientation;
        public int LODLevel;
    }

    public class FCardRepresentationData
    {
        public FBox Bounds;
        public int MaxLodLevel;
        public FLumenCardBuildData[] CardBuildData;

        public FCardRepresentationData(FArchive Ar)
        {
            Bounds = Ar.Read<FBox>();
            MaxLodLevel = Ar.Read<int>();
            CardBuildData = Ar.ReadArray<FLumenCardBuildData>();
        }
    }
}