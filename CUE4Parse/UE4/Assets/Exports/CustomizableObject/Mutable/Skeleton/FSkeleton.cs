using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeleton;

public class FSkeleton : IMutablePtr
{
    public FBoneName[] BoneIds;
    public short[] BoneParents;

    public bool IsBroken { get; set; }

    public FSkeleton(FArchive Ar)
    {
        var version = Ar.Read<int>();
        if (version == -1)
        {
            IsBroken = true;
            return;
        }

        BoneIds = Ar.ReadArray(() => new FBoneName(Ar));
        BoneParents = Ar.ReadArray<short>();
    }
}
