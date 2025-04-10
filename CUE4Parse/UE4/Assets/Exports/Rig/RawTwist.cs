using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawTwist
{
    public float[] TwistBlendWeights;
    public ushort[] TwistOutputJointIndices;
    public ushort[] TwistInputControlIndices;
    public ETwistAxis TwistAxis;

    public RawTwist(FArchiveBigEndian Ar)
    {
        TwistBlendWeights = Ar.ReadArray<float>();
        TwistOutputJointIndices = Ar.ReadArray<ushort>();
        TwistInputControlIndices = Ar.ReadArray<ushort>();
        TwistAxis = (ETwistAxis) Ar.Read<ushort>();
    }
}
