using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FBoxBody : FBodyShape
{
    public int Version;
    public FVector Position;
    public FQuat Orientation;
    public FVector Size;
    
    public FBoxBody(FAssetArchive Ar) : base(Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 0)
            throw new NotSupportedException($"Mutable FBoxBody Version '{Version}' is currently not supported");
        
        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Size = Ar.Read<FVector>();
    }
}