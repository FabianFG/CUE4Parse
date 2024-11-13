using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics;

public class FPhysicsBodyAggregate
{
    public FSphereBody[] Spheres;
    public FBoxBody[] Boxes;
    public FConvexBody[] Convex;
    public FSphylBody[] Sphyls;
    public FTaperedCapsuleBody[] TaperedCapsules;

    public FPhysicsBodyAggregate(FArchive Ar)
    {
        var version = Ar.Read<int>();

        Spheres = Ar.ReadArray(() => new FSphereBody(Ar));
        Boxes = Ar.ReadArray(() => new FBoxBody(Ar));
        Convex = Ar.ReadArray(() => new FConvexBody(Ar));
        Sphyls = Ar.ReadArray(() => new FSphylBody(Ar));
        TaperedCapsules = Ar.ReadArray(() => new FTaperedCapsuleBody(Ar));
    }
}
