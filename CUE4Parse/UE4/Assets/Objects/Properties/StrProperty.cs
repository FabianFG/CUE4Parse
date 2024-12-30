﻿using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(StrPropertyConverter))]
public class StrProperty : FPropertyTagType<string>
{
    public StrProperty(string value)
    {
        Value = value;
    }

    public StrProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Ar.ReadFString()
        };
    }
}