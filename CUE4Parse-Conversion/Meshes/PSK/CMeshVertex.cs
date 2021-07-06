using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CMeshVertex
    {
        public FVector Position;
        public FPackedNormal Normal;
        public FPackedNormal Tangent;
        public FMeshUVFloat UV;

        public CMeshVertex(FVector position, FPackedNormal normal, FPackedNormal tangent, FMeshUVFloat uv)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            UV = uv;
        }
    }
}