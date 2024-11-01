using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FRangeDesc
{
    public int Version;
    public string Name;
    public string Uid;
    public int DimensionParameter;

    public FRangeDesc(FArchive Ar)
    {
        Version = Ar.Read<int>();
        if (Version > 3)
            throw new NotSupportedException($"Mutable FRangeDesc Version '{Version}' is currently not supported");
        
        Name = Ar.ReadMutableFString();
        Uid = Ar.ReadMutableFString();
        DimensionParameter = Ar.Read<int>();
    }
}