using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CStaticMeshLod : CBaseMeshLod
    {
        public CMeshVertex[] Verts;

        public void AllocateVerts(int count)
        {
            Verts = new CMeshVertex[count];
            for (var i = 0; i < Verts.Length; i++)
            {
                Verts[i] = new CMeshVertex(new FVector(), new FPackedNormal(0), new FPackedNormal(0), new FMeshUVFloat(0, 0));
            }

            NumVerts = count;
            AllocateUVBuffers();
        }

        public void BuildNormals()
        {
            if (HasNormals) return;
            // BuildNormalsCommon(Verts, Indices);
            HasNormals = true;
        }
    }
}
