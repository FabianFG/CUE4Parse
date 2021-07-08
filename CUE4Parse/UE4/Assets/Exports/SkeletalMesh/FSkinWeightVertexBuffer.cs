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
            var bNewWeightFormat = FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences;

            #region FSkinWeightDataVertexBuffer
            var dataStripFlags = Ar.Read<FStripDataFlags>();

            #region FSkinWeightDataVertexBuffer::SerializeMetaData
            bool bVariableBonesPerVertex;
            uint maxBoneInfluences;
            bool bUse16BitBoneIndex;
            uint numVertices;
            uint numBones;

            if (!bNewWeightFormat)
            {
                var bExtraBoneInfluences = Ar.ReadBoolean();
                if (FSkeletalMeshCustomVersion.Get(Ar) >= FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
                {
                    Ar.Position += 4; //var stride = Ar.Read<uint>();
                }
                numVertices = Ar.Read<uint>();
                maxBoneInfluences = bExtraBoneInfluences ? 8u : 4u;
                numBones = maxBoneInfluences * numVertices;
                bVariableBonesPerVertex = false;
            }
            else
            {
                bVariableBonesPerVertex = Ar.ReadBoolean();
                maxBoneInfluences = Ar.Read<uint>();
                numBones = Ar.Read<uint>();
                numVertices = Ar.Read<uint>();
            }

            // bUse16BitBoneIndex doesn't exist before version IncreaseBoneIndexLimitPerChunk
            if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.IncreaseBoneIndexLimitPerChunk)
            {
                bUse16BitBoneIndex = Ar.ReadBoolean();
            }
            #endregion

            byte[] newData = Array.Empty<byte>();
            if (!dataStripFlags.IsDataStrippedForServer())
            {
                if (!bNewWeightFormat)
                {
                    Weights = Ar.ReadBulkArray(() => new FSkinWeightInfo(Ar, maxBoneInfluences > _NUM_INFLUENCES_UE4));
                }
                else
                {
                    newData = Ar.ReadBulkArray<byte>();
                }
            }
            #endregion

            if (bNewWeightFormat)
            {
                #region FSkinWeightLookupVertexBuffer
                var lookupStripFlags = Ar.Read<FStripDataFlags>();

                #region FSkinWeightLookupVertexBuffer::SerializeMetaData
                //if (bNewWeightFormat)
                //{
                var numLookupVertices = Ar.Read<int>();
                //}
                #endregion

                if (!lookupStripFlags.IsDataStrippedForServer())
                {
                    Ar.ReadBulkArray<uint>(); // LookupData
                }
                #endregion

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