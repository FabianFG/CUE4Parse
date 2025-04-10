using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawJointGroup
{
    public ushort[] LODs;
    public ushort[] InputIndices;
    public ushort[] OutputIndices;
    public float[] Values;
    public ushort[] JointIndices;

    public RawJointGroup(FArchiveBigEndian Ar)
    {
        LODs = Ar.ReadArray<ushort>();
        InputIndices = Ar.ReadArray<ushort>();
        OutputIndices = Ar.ReadArray<ushort>();
        Values = Ar.ReadArray<float>();
        JointIndices = Ar.ReadArray<ushort>();
    }
}
