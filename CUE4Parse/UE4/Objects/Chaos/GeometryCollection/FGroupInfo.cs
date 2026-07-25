using System.Diagnostics;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly record struct FGroupInfo
{
    public readonly int Size;

    public FGroupInfo()
    {
        Size = 0;
    }

    public FGroupInfo(FChaosArchive Ar)
    {
        var version = Ar.Read<int>();
        if (version > 4) throw new ParserException(Ar, $"FGroupInfo Version ({version}) > 4");
        
        Size = Ar.Read<int>();
    }
}