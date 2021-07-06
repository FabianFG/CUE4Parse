using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkinWeightVertexBuffer
    {
        private const int _NUM_INFLUENCES_UE4 = 4;
        
        public readonly FSkinWeightInfo[] Weights;

        public FSkinWeightVertexBuffer(FAssetArchive Ar, bool numSkelCondition)
        {
            var skinWeightStripFlags = Ar.Read<FStripDataFlags>();
            
            int numVertices, stride = 0;
            bool bExtraBoneInfluences, bVariableBonesPerVertex, bUse16BitBoneIndex = false;
            var bNewWeightFormat = FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences;
            
            if (Ar.Game < EGame.GAME_UE4_24)
            {
                bExtraBoneInfluences = Ar.ReadBoolean();
                numVertices = Ar.Read<int>();
            }
            else if (!bNewWeightFormat)
            {
                bExtraBoneInfluences = Ar.ReadBoolean();
                stride = Ar.Read<int>();
                numVertices = Ar.Read<int>();
            }
            else
            {
                bVariableBonesPerVertex = Ar.ReadBoolean();
                var maxBoneInfluences = Ar.Read<int>();
                var numBones = Ar.Read<int>();
                numVertices = Ar.Read<int>();
                bExtraBoneInfluences = maxBoneInfluences > _NUM_INFLUENCES_UE4;
                if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.IncreaseBoneIndexLimitPerChunk)
                    bUse16BitBoneIndex = Ar.ReadBoolean();
            }

            byte[] newData = Array.Empty<byte>();
            if (!skinWeightStripFlags.IsDataStrippedForServer())
            {
                if (!bNewWeightFormat)
                {
                    Weights = Ar.ReadArray(() => new FSkinWeightInfo(Ar, bExtraBoneInfluences));
                }
                else
                {
                    newData = Ar.ReadBulkArray<byte>();
                }
            }

            if (bNewWeightFormat)
            {
                var lookupStripFlags = Ar.Read<FStripDataFlags>();
                var numLookupVertices = Ar.Read<int>();
                
                if (!lookupStripFlags.IsDataStrippedForServer())
                    Ar.ReadArray<uint>(); // LookupVertexBuffer
                
                // Convert influence data
                if (newData.Length > 0)
                {
                    using var tempAr = new FByteArchive("WeightsReader", newData, Ar.Game, Ar.Ver);
                    Weights = new FSkinWeightInfo[numVertices];
                    for (var i = 0; i < Weights.Length; i++)
                    {
                        Weights[i] = new FSkinWeightInfo(tempAr, numSkelCondition);
                    }
                }
            }
        }

        public static int MetadataSize(FAssetArchive Ar)
        {
            var numBytes = 0;
            var bNewWeightFormat = FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences;
            
            if (Ar.Game < EGame.GAME_UE4_24)
            {
                numBytes = 2 * 4;
            }
            else if (!bNewWeightFormat)
            {
                numBytes = 3 * 4;
            }
            else
            {
                numBytes = 4 * 4;
                if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.IncreaseBoneIndexLimitPerChunk)
                    numBytes += 4;
            }
            
            if (bNewWeightFormat)
            {
                numBytes += 4;
            }
            
            return numBytes;
        }
    }
}