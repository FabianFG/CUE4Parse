using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkeletalMesh
    {
        public List<CSkelMeshLod> LODs;
        public List<CSkelMeshBone> RefSkeleton;
        public FBox BoundingBox;
        public FSphere BoundingSphere;

        public CSkeletalMesh()
        {
            LODs = new List<CSkelMeshLod>();
            RefSkeleton = new List<CSkelMeshBone>();
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
    }
}
