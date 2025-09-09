using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkeletalMesh : IDisposable
    {
        public readonly List<CSkelMeshLod> LODs = [];
        public readonly List<CSkelMeshBone> RefSkeleton = [];
        
        public FBox BoundingBox;
        public FSphere BoundingSphere;

        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail?.BuildNormals();
            }

            // SortBones();
            // FixBoneWeights();
        }

        public void Dispose()
        {
            foreach (var lod in LODs)
                lod.Dispose();
            
            LODs.Clear();
            RefSkeleton.Clear();
        }
    }
}
