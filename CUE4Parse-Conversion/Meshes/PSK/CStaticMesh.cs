namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CStaticMesh
    {
        public CStaticMeshLod[] LODs;

        public CStaticMesh()
        {
            LODs = new CStaticMeshLod[0];
        }
        
        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail.BuildNormals();
            }
        }
    }
}