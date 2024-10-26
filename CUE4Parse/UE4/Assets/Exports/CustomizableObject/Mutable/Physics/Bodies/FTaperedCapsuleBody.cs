using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FTaperedCapsuleBody : FBodyShape
{
    public int Version;
    public FVector Position;
    public FQuat Orientation;
    public float Radius0;
    public float Radius1;
    public float Length;
    
    public FTaperedCapsuleBody(FAssetArchive Ar) : base(Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 0)
            throw new NotSupportedException($"Mutable FPhysicsBodyAggregate Version '{Version}' is currently not supported");
        
        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Radius0 = Ar.Read<float>();
        Radius1 = Ar.Read<float>();
        Length = Ar.Read<float>();
    }
}