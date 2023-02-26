using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkinWeightInfo
    {
        private const int NUM_INFLUENCES_UE4 = 4;
        private const int MAX_TOTAL_INFLUENCES_UE4 = 8;

        public readonly ushort[] BoneIndex;
        public readonly byte[] BoneWeight;

        public FSkinWeightInfo()
        {
            BoneIndex = new ushort[NUM_INFLUENCES_UE4];
            BoneWeight = new byte[NUM_INFLUENCES_UE4];
        }

        public FSkinWeightInfo(FArchive Ar, bool bExtraBoneInfluences, bool bUse16BitBoneIndex = false, int length = 0)
        {
            var numSkelInfluences = bExtraBoneInfluences ? MAX_TOTAL_INFLUENCES_UE4 : NUM_INFLUENCES_UE4;
            if (length > 0) numSkelInfluences = length;

            BoneIndex = bUse16BitBoneIndex ? Ar.ReadArray<ushort>(numSkelInfluences) : Ar.ReadArray(numSkelInfluences, () => (ushort)Ar.Read<byte>());
            BoneWeight = Ar.ReadArray<byte>(numSkelInfluences);        
        }
    }
}
