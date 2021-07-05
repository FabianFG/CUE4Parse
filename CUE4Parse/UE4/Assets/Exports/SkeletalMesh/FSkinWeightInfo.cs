using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkinWeightInfo
    {
        private const int _NUM_INFLUENCES_UE4 = 4;
        private const int _MAX_TOTAL_INFLUENCES_UE4 = 8;
        
        public readonly byte[] BoneIndex;
        public readonly byte[] BoneWeight;

        public FSkinWeightInfo()
        {
            BoneIndex = new byte[_NUM_INFLUENCES_UE4];
            BoneWeight = new byte[_NUM_INFLUENCES_UE4];
        }
        
        public FSkinWeightInfo(FArchive Ar, bool numSkelCondition) : this()
        {
            var numSkelInfluences = numSkelCondition ? _MAX_TOTAL_INFLUENCES_UE4 : _NUM_INFLUENCES_UE4;
            if (numSkelInfluences <= BoneIndex.Length)
            {
                for (var i = 0; i < numSkelInfluences; i++)
                    BoneIndex[i] = Ar.Read<byte>();
                for (var i = 0; i < numSkelInfluences; i++)
                    BoneWeight[i] = Ar.Read<byte>();
            }
            else
            {
                var boneIndex2 = new byte[_NUM_INFLUENCES_UE4];
                var boneWeight2 = new byte[_NUM_INFLUENCES_UE4];
                for (var i = 0; i < numSkelInfluences; i++)
                    boneIndex2[i] = Ar.Read<byte>();
                for (var i = 0; i < numSkelInfluences; i++)
                    boneWeight2[i] = Ar.Read<byte>();
                
                // copy influences to vertex
                for (var i = 0; i < _NUM_INFLUENCES_UE4; i++)
                {
                    BoneIndex[i] = boneIndex2[i];
                    BoneWeight[i] = boneWeight2[i];
                }
            }
        }
    }
}