using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FApexClothPhysToRenderVertData
    {
        public readonly FVector4 PositionBaryCoordsAndDist;
        public readonly FVector4 NormalBaryCoordsAndDist;
        public readonly FVector4 TangentBaryCoordsAndDist;
        public readonly short[] SimulMeshVertIndices;
        public readonly int[] Padding;

        public FApexClothPhysToRenderVertData(FAssetArchive Ar)
        {
            PositionBaryCoordsAndDist = Ar.Read<FVector4>();
            NormalBaryCoordsAndDist = Ar.Read<FVector4>();
            TangentBaryCoordsAndDist = Ar.Read<FVector4>();
            SimulMeshVertIndices = Ar.ReadArray(4, Ar.Read<short>);
            Padding = Ar.ReadArray(2, Ar.Read<int>);
        }
    }
}