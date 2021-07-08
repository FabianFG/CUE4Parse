namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkeletalMesh
    {
        public CSkelMeshLod[] LODs;
        public CSkelMeshBone[] RefSkeleton;
        
        public CSkeletalMesh()
        {
            LODs = new CSkelMeshLod[0];
            RefSkeleton = new CSkelMeshBone[0];
        }
        
        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail.BuildNormals();
            }
            
            // SortBones();
            // FixBoneWeights();
        }
    }
}