using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FConvexBody : FBodyShape
{
    public int Version;
    public FVector[] Vertices;
    public int[] Indices;
    public FTransform Transform;
    
    public FConvexBody(FArchive Ar) : base(Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 0)
            throw new NotSupportedException($"Mutable FConvexBody Version '{Version}' is currently not supported");

        Vertices = Ar.ReadArray<FVector>();
        Indices = Ar.ReadArray<int>();
        Transform = Ar.Read<FTransform>();
    }
}