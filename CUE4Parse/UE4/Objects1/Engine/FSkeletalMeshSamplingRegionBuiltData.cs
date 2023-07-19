using System;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine
{
    /** Built data for sampling a single region of a skeletal mesh. */
    public readonly struct FSkeletalMeshSamplingRegionBuiltData : IUStruct 
    {
        /** Triangles included in this region. */
        public readonly int[] TriangleIndices;
        /** Vertices included in this region. */
        public readonly int[] Vertices;
        /** Bones included in this region. */
        public readonly int[] BoneIndices;
        /** Provides random area weighted sampling of the TriangleIndices array. */
        public readonly FSkeletalMeshAreaWeightedTriangleSampler AreaWeightedSampler;

        public FSkeletalMeshSamplingRegionBuiltData(FArchive Ar)
        {
            TriangleIndices = Ar.ReadArray<int>();
            BoneIndices = Ar.ReadArray<int>();
            AreaWeightedSampler = new FSkeletalMeshAreaWeightedTriangleSampler(Ar);
            Vertices = FNiagaraObjectVersion.Get(Ar) >= FNiagaraObjectVersion.Type.SkeletalMeshVertexSampling ? Ar.ReadArray<int>() : Array.Empty<int>();
        }
    }
}