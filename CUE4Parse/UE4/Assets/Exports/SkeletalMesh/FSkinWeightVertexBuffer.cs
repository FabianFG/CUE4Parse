using System;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh
{
    public class FSkinWeightVertexBuffer
    {
        private const int _NUM_INFLUENCES_UE4 = 4;

        public readonly FSkinWeightInfo[] Weights;

        public FSkinWeightVertexBuffer(FArchive Ar, bool numSkelCondition)
        {
            var bNewWeightFormat = FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences;

            #region FSkinWeightDataVertexBuffer
            var dataStripFlags = Ar.Read<FStripDataFlags>();

            #region FSkinWeightDataVertexBuffer::SerializeMetaData
            bool bVariableBonesPerVertex = false;
            bool bExtraBoneInfluences;
            uint maxBoneInfluences;
            bool bUse16BitBoneIndex = false;
            bool bUse16BitBoneWeight;
            uint numVertices;
            uint numBones;

            if (!Ar.Versions["SkeletalMesh.UseNewCookedFormat"])
            {
                bExtraBoneInfluences = Ar.ReadBoolean();
                numVertices = Ar.Read<uint>();
                maxBoneInfluences = bExtraBoneInfluences ? 8u : 4u;
            }
            else if (!bNewWeightFormat)
            {
                bExtraBoneInfluences = Ar.ReadBoolean();
                if (FSkeletalMeshCustomVersion.Get(Ar) >= FSkeletalMeshCustomVersion.Type.SplitModelAndRenderData)
                {
                    Ar.Position += 4; // var stride = Ar.Read<uint>();
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
                bExtraBoneInfluences = maxBoneInfluences > _NUM_INFLUENCES_UE4;
                // bUse16BitBoneIndex doesn't exist before version IncreaseBoneIndexLimitPerChunk
                if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.IncreaseBoneIndexLimitPerChunk)
                {
                    bUse16BitBoneIndex = Ar.ReadBoolean();
                }
                if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.IncreasedSkinWeightPrecision)
                {
                    bUse16BitBoneWeight = Ar.ReadBoolean();
                }
            }
            #endregion

            byte[] newData = Array.Empty<byte>();
            if (!dataStripFlags.IsAudioVisualDataStripped())
            {
                if (!bNewWeightFormat)
                {
                    Weights = Ar.ReadBulkArray(() => new FSkinWeightInfo(Ar, bExtraBoneInfluences, bUse16BitBoneIndex));
                }
                else
                {
                    newData = Ar.ReadBulkArray<byte>();
                }
            }
            else
            {
                bExtraBoneInfluences = numSkelCondition;
            }
            #endregion

            if (bNewWeightFormat)
            {
                uint[] LookupData = Array.Empty<uint>();

                var lookupStripFlags = Ar.Read<FStripDataFlags>();
                var numLookupVertices = Ar.Read<int>();

                if (!lookupStripFlags.IsAudioVisualDataStripped())
                {
                    LookupData = Ar.ReadBulkArray<uint>();
                }

                // Convert influence data
                if (newData.Length > 0)
                {
                    using var tempAr = new FByteArchive("WeightsReader", newData, Ar.Versions);
                    Weights = new FSkinWeightInfo[numVertices];

                    if (bVariableBonesPerVertex)
                    {
                        if (LookupData.Length != numVertices)
                            throw new ParserException($"LookupData NumVertices={LookupData.Length} != NumVertices={numVertices}");

                        for (var i = 0; i < Weights.Length; i++)
                        {
                            tempAr.Position = LookupData[i] >> 8;
                            Weights[i] = new FSkinWeightInfo(tempAr, bExtraBoneInfluences, bUse16BitBoneIndex, (byte)LookupData[i]);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < Weights.Length; i++)
                        {
                            Weights[i] = new FSkinWeightInfo(tempAr, bExtraBoneInfluences, bUse16BitBoneIndex);
                        }
                    }
                }
            }

            Weights ??= Array.Empty<FSkinWeightInfo>();
        }

        public static int MetadataSize(FArchive Ar)
        {
            var numBytes = 0;
            var bNewWeightFormat = FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.UnlimitedBoneInfluences;

            if (!Ar.Versions["SkeletalMesh.UseNewCookedFormat"])
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
                if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.IncreasedSkinWeightPrecision)
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
