using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine
{
    public readonly struct FSkeletalMeshSamplingLODBuiltData : IUStruct
    {
        /** Area weighted sampler for the whole mesh at this LOD.*/
        public readonly FSkeletalMeshAreaWeightedTriangleSampler AreaWeightedTriangleSampler;

        public FSkeletalMeshSamplingLODBuiltData(FAssetArchive Ar)
        {
            AreaWeightedTriangleSampler = new FSkeletalMeshAreaWeightedTriangleSampler(Ar);
        }
    }

    public class FSkeletalMeshAreaWeightedTriangleSampler : FWeightedRandomSampler
    {
        //Data used in initialization of the sampler. Not serialized.
        //public readonly USkeletalMesh Owner;
        //public readonly int[] TriangleIndices;
        //public readonly int LODIndex;

        public FSkeletalMeshAreaWeightedTriangleSampler(FAssetArchive Ar) : base(Ar) { }
    }

    public class FWeightedRandomSampler : IUClass
    {
        public readonly float[] Prob;
        public readonly int[] Alias;
        public readonly float TotalWeight;

        public FWeightedRandomSampler(FAssetArchive Ar)
        {
            Prob = Ar.ReadArray<float>();
            Alias = Ar.ReadArray<int>();
            TotalWeight = Ar.Read<float>();
        }
    }
}
