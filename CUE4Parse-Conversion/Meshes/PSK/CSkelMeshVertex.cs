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
            Bone = new short[4];
        }

        public float[] UnpackWeights()
        {
            var ret = new float[4];
            var scale = 1.0f / 255;
            ret[0] =  (PackedWeights        & 0xFF) * scale;
            ret[1] = ((PackedWeights >> 8 ) & 0xFF) * scale;
            ret[2] = ((PackedWeights >> 16) & 0xFF) * scale;
            ret[3] = ((PackedWeights >> 24) & 0xFF) * scale;
            return ret;
        }
    }
}