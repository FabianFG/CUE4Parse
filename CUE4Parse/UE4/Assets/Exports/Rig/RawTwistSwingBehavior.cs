using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class RawTwistSwingBehavior : IRawBase
{
    public RawTwist[] Twists;
    public RawTwist[] Swings;

    public RawTwistSwingBehavior(FArchiveBigEndian Ar)
    {
        Twists = Ar.ReadArray(() => new RawTwist(Ar));
        Swings = Ar.ReadArray(() => new RawTwist(Ar));
    }
}
