using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FBodyShape
{
    public string Name;
    public uint Flags;

    public FBodyShape(FArchive Ar)
    {
        var version = Ar.Read<int>();

        Name = Ar.ReadMutableFString();
        Flags = Ar.Read<uint>();
    }
}
