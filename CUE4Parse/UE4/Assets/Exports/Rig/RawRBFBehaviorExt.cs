using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawRBFBehaviorExt : IRawBase
{
    public string[] PoseControlNames;
    public RawRBFPoseExt[] Poses;

    public RawRBFBehaviorExt(FArchiveBigEndian Ar)
    {
        PoseControlNames = Ar.ReadArray(Ar.ReadString);
        Poses = Ar.ReadArray(() => new RawRBFPoseExt(Ar));
    }
}
