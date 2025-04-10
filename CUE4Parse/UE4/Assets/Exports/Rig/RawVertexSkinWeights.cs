using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawVertexSkinWeights
{
    public float[] Weights;
    public ushort[] JointIndices;

    public RawVertexSkinWeights(FArchiveBigEndian Ar)
    {
        Weights = Ar.ReadArray<float>();
        JointIndices = Ar.ReadArray<ushort>();
    }
}
