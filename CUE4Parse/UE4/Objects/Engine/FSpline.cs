using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Engine;

public struct FSpline : IUStruct
{
    public FSpline(FArchive Ar)
    {
        var previousImpl  = Ar.Read<sbyte>();

        var wasEnabled = previousImpl  != 0;
        var wasLegacy = previousImpl == 1;
        
        if (wasEnabled)
        {
            // TODO:
            throw new NotSupportedException("Further serialization of FSpline Struct type is currently not supported");
        }
    }
}