using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class VVertex
    {
        public readonly int PointIndex;
        public readonly FMeshUVFloat UV;
        public readonly byte MatIndex;
        public readonly byte Reserved;
        public readonly short Pad;

        public VVertex(int pointIndex, FMeshUVFloat uv, byte matIndex, byte reserved, short pad)
        {
            PointIndex = pointIndex;
            UV = uv;
            MatIndex = matIndex;
            Reserved = reserved;
            Pad = pad;
        }

        public void Serialize(FArchiveWriter writer)
        {
            writer.Write(PointIndex);
            writer.Write(UV.U);
            writer.Write(UV.V);
            writer.Write(MatIndex);
            writer.Write(Reserved);
            writer.Write(Pad);
        }
    }
}