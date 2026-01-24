using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics;

public class FPhysicsBodyAggregate
{
    public FSphereBody[] Spheres;
    public FBoxBody[] Boxes;
    public FConvexBody[] Convexes;
    public FSphylBody[] Sphyls;
    public FTaperedCapsuleBody[] TaperedCapsules;
    
    public FPhysicsBodyAggregate(FMutableArchive Ar)
    {
        Spheres = Ar.ReadArray(() => new FSphereBody(Ar));
        Boxes = Ar.ReadArray(() => new FBoxBody(Ar));
        Convexes = Ar.ReadArray(() => new FConvexBody(Ar));
        Sphyls = Ar.ReadArray(() => new FSphylBody(Ar));
        TaperedCapsules = Ar.ReadArray(() => new FTaperedCapsuleBody(Ar));
    }
}