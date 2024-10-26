using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FSphereBody : FBodyShape
{
    public int Version;
    public FVector Position;
    public float Radius;
    
    public FSphereBody(FAssetArchive Ar) : base(Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 0)
            throw new NotSupportedException($"Mutable FSphereBody Version '{Version}' is currently not supported");

        Position = Ar.Read<FVector>();
        Radius = Ar.Read<float>();
    }
}