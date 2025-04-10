using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawJointBehaviorMetadata : IRawBase
{
    public RawJointRepresentation[] JointRepresentations;

    public RawJointBehaviorMetadata(FArchiveBigEndian Ar)
    {
        JointRepresentations = Ar.ReadArray(() => new RawJointRepresentation(Ar));
    }
}
