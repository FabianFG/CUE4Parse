using System;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Materials;

public class FImageParameterData
{
    public object ImageParameter;
    public int ImagePropertyIndex;

    public FImageParameterData(FMutableArchive Ar)
    {
        throw new NotSupportedException("Serialization of ImageParameter is not supported");
        ImagePropertyIndex = Ar.Read<int>();
    }
}