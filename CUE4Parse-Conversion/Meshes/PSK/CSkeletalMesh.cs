using System;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkeletalMesh
    {
        private const int _NUM_INFLUENCES = 4;
        
        public CSkelMeshLod?[] LODs;
        public CSkelMeshBone[] RefSkeleton;
        public FBox BoundingBox;
        public FSphere BoundingShere;
        
        public CSkeletalMesh()
        {
            LODs = new CSkelMeshLod[0];
            RefSkeleton = new CSkelMeshBone[0];
        }
        
        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail?.BuildNormals();
            }
            
            // SortBones();
            // FixBoneWeights();
        }

        public void FixBoneWeights()
        {
            for (var lod = 0; lod < LODs.Length; lod++)
            {
                for (var vert = 0; vert < LODs[lod].NumVerts; vert++)
                {
                    var shouldFix = false;
                    var unpackedWeights = BitConverter.GetBytes(LODs[lod].Verts[vert].PackedWeights);
                    for (var i = 0; i < _NUM_INFLUENCES; i++)
                    {
                        int bone = LODs[lod].Verts[vert].Bone[i];
                        if (bone < 0) break;
                        
                        if (unpackedWeights[i] == 0)
                        {
                            // remove zero weight
                            shouldFix = true;
                            continue;
                        }
                        
                        // remove duplicated influences, if any
                        for (var j = 0; j < i; j++)
                        {
                            if (LODs[lod].Verts[vert].Bone[j] == bone)
                            {
                                // add k's weight to i, and set k's weight to 0
                                var newWeight = unpackedWeights[i] + unpackedWeights[j];
                                if (newWeight > 255) newWeight = 255;
                                unpackedWeights[i] = (byte)(newWeight & 0xFF);
                                unpackedWeights[j] = 0;
                                shouldFix = true;
                            }
                        }
                    }
                    
                    if (shouldFix)
                    {
                        for (var i = _NUM_INFLUENCES - 1; i >= 0; i--) // iterate in reverse order for correct removal of '0' followed by '0'
                        {
                            if (unpackedWeights[i] == 0)
                            {
                                if (i < _NUM_INFLUENCES - 1)
                                {
                                    // not very fast, but shouldn't do that too often
                                    // memcpy(unpackedWeights + i, unpackedWeights + i + 1, _NUM_INFLUENCES - i - 1);
                                    // memcpy(LODs[lod].Verts[vert].Bone + i, LODs[lod].Verts[vert].Bone + i + 1, (_NUM_INFLUENCES - i - 1) * 2);
                                    throw new NotImplementedException();
                                }
                                // remove last weight item
                                unpackedWeights[_NUM_INFLUENCES - 1] = 0;
                                LODs[lod].Verts[vert].Bone[_NUM_INFLUENCES - 1] = -1;
                            }
                        }
                        // pack weights back to vertex
                        LODs[lod].Verts[vert].PackedWeights = BitConverter.ToUInt32(unpackedWeights);
                    }
                    
                    var totalWeight = 0;
                    int numInfluences;
                    for (numInfluences = 0; numInfluences < _NUM_INFLUENCES; numInfluences++)
                    {
                        if (LODs[lod].Verts[vert].Bone[numInfluences] < 0) break;
                        totalWeight += unpackedWeights[numInfluences];
                    }
                    
                    if (totalWeight != 255)
                    {
                        // Do renormalization
                        var scale = 255.0f / totalWeight;
                        totalWeight = 0;
                        
                        for (var i = 0; i < numInfluences; i++)
                        {
                            unpackedWeights[i] = (byte)Math.Round(unpackedWeights[i] * scale);
                            totalWeight += unpackedWeights[i];
                        }
                        // There still could be TotalWeight which differs slightly from value 255.
                        // Adjust first bone weight to make sum matching 255. Assume that the first
                        // weight is largest one (it is true at least for UE4), so this adjustment
                        // won't be noticeable.
                        unpackedWeights[0] += (byte)(255 - totalWeight);

                        LODs[lod].Verts[vert].PackedWeights = BitConverter.ToUInt32(unpackedWeights);
                    }
                }
            }
        }
    }
}