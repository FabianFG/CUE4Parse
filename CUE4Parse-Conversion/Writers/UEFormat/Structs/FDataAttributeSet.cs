using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Writers.UEFormat.Structs;

public class FDataAttributeSet : ISerializable
{
    private readonly List<FDataAttribute> _attributes = [];

    public void AddAttribute(string name, Action<FDataAttribute> write)
    {
        var attribute = new FDataAttribute(name);
        write(attribute);
        _attributes.Add(attribute);
    }

    public void Serialize(FArchiveWriter Ar) => Ar.WriteArray(_attributes);
}
