using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeleton;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics;

public class FPhysicsBody : IMutablePtr
{
    public int CustomId;
    public FPhysicsBodyAggregate[] Bodies;
    public FBoneName[] BoneIds;
    public int[] BodiesCustomIds;
    public bool bBodiesModified;

    public bool IsBroken { get; set; }

    public FPhysicsBody(FArchive Ar)
    {
        var version = Ar.Read<int>();
        if (version == -1)
        {
            IsBroken = true;
            return;
        }

        CustomId = Ar.Read<int>();
        Bodies = Ar.ReadArray(() => new FPhysicsBodyAggregate(Ar));
        BoneIds = Ar.ReadArray(() => new FBoneName(Ar));
        BodiesCustomIds = Ar.ReadArray<int>();
        bBodiesModified = Ar.ReadFlag();
    }
}
