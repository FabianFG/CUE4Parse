using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FRuntimeSkinWeightProfileData
    {
        public readonly Dictionary<uint, uint> VertexIndexToInfluenceOffset;
        public readonly FSkinWeightOverrideInfo[] OverridesInfo;
        public readonly ushort[] Weights;
        public readonly byte[] BoneIDs;
        public readonly byte[] BoneWeights;
        public readonly byte NumWeightsPerVertex;

        public FRuntimeSkinWeightProfileData(FArchive Ar)
        {
            if (Ar.Ver < EUnrealEngineObjectUE4Version.SKINWEIGHT_PROFILE_DATA_LAYOUT_CHANGES)
            {
                OverridesInfo = Ar.ReadArray<FSkinWeightOverrideInfo>();
                Weights = Ar.ReadArray<ushort>();
            }
            else
            {
                // UE4.26+
                BoneIDs = Ar.ReadArray<byte>();
                BoneWeights = Ar.ReadArray<byte>();
                NumWeightsPerVertex = Ar.Read<byte>();
            }
            
            var length = Ar.Read<int>();
            VertexIndexToInfluenceOffset = new Dictionary<uint, uint>();
            for (var i = 0; i < length; i++)
            {
                VertexIndexToInfluenceOffset[Ar.Read<uint>()] = Ar.Read<uint>();
            }
        }
    }
}