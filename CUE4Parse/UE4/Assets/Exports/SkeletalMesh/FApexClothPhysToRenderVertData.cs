using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FApexClothPhysToRenderVertData
    {
        public readonly FVector4 PositionBaryCoordsAndDist;
        public readonly FVector4 NormalBaryCoordsAndDist;
        public readonly FVector4 TangentBaryCoordsAndDist;
        public readonly short[] SimulMeshVertIndices;
        public readonly int[] Padding;

        public FApexClothPhysToRenderVertData(FArchive Ar)
        {
            PositionBaryCoordsAndDist = Ar.Read<FVector4>();
            NormalBaryCoordsAndDist = Ar.Read<FVector4>();
            TangentBaryCoordsAndDist = Ar.Read<FVector4>();
            SimulMeshVertIndices = Ar.ReadArray<short>(4);
            Padding = Ar.ReadArray<int>(2);
        }
    }
}