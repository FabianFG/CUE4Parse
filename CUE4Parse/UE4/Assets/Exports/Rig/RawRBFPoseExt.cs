using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawRBFPoseExt
{
    public ushort[] InputControlIndices;
    public ushort[] OutputControlIndices;
    public float[] OutputControlWeights;

    public RawRBFPoseExt(FArchiveBigEndian Ar)
    {
        InputControlIndices = Ar.ReadArray<ushort>();
        OutputControlIndices = Ar.ReadArray<ushort>();
        OutputControlWeights = Ar.ReadArray<float>();
    }
}
