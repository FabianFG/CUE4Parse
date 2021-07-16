using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CStaticMesh
    {
        public CStaticMeshLod?[] LODs;
        public FBox BoundingBox;
        public FSphere BoundingShere;

        public CStaticMesh()
        {
            LODs = new CStaticMeshLod[0];
        }
        
        public void FinalizeMesh()
        {
            foreach (var levelOfDetail in LODs)
            {
                levelOfDetail?.BuildNormals();
            }
        }
    }
}