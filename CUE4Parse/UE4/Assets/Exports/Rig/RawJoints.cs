using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawJoints
{
    public ushort RowCount;
    public ushort ColCount;
    public RawJointGroup[] JointGroups;

    public RawJoints(FArchiveBigEndian Ar)
    {
        RowCount = Ar.Read<ushort>();
        ColCount = Ar.Read<ushort>();
        JointGroups = Ar.ReadArray(() => new RawJointGroup(Ar));
    }
}
