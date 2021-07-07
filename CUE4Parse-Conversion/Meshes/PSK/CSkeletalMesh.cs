namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkeletalMesh
    {
        public CSkelMeshLod[] LODs;
        
        public CSkeletalMesh()
        {
            LODs = new CSkelMeshLod[0];
        }
        
        public CSkeletalMesh(CSkelMeshLod[] lods)
        {
            LODs = lods;
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