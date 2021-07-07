using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CSkelMeshVertex : CMeshVertex
    {
        public uint PackedWeights;
        public short[]? Bone;

        public CSkelMeshVertex(FVector position, FPackedNormal normal, FPackedNormal tangent, FMeshUVFloat uv) : base(position, normal, tangent, uv)
        {
        }
    }
}