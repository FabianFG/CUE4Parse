using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FBodyShape
{
    public int Version;
    public string Name;
    public uint Flags;
    
    public FBodyShape(FArchive Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 1)
            throw new NotSupportedException($"Mutable FBodyShape Version '{Version}' is currently not supported");
                
        Name = Ar.ReadMutableFString();
        Flags = Ar.Read<uint>();
    }
}