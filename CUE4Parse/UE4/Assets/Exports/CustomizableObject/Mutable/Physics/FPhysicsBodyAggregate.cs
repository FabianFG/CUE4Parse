using System;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics;

public class FPhysicsBodyAggregate
{
    public int Version;
    public FSphereBody[] Spheres;
    public FBoxBody[] Boxes;
    public FConvexBody[] Convex;
    public FSphylBody[] Sphyls;
    public FTaperedCapsuleBody[] TaperedCapsules;
    
    public FPhysicsBodyAggregate(FAssetArchive Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 0)
            throw new NotSupportedException($"Mutable FPhysicsBodyAggregate Version '{Version}' is currently not supported");

        Spheres = Ar.ReadArray(() => new FSphereBody(Ar));
        Boxes = Ar.ReadArray(() => new FBoxBody(Ar));
        Convex = Ar.ReadArray(() => new FConvexBody(Ar));
        Sphyls = Ar.ReadArray(() => new FSphylBody(Ar));
        TaperedCapsules = Ar.ReadArray(() => new FTaperedCapsuleBody(Ar));
    }
}