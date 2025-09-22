using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(BoolPropertyConverter))]
public class BoolProperty : FPropertyTagType<bool>
{
    public BoolProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
    {
        Value = type switch
        {
            ReadType.NORMAL when !Ar.HasUnversionedProperties => tagData?.Bool == true,
            ReadType.NORMAL or ReadType.MAP or ReadType.ARRAY or ReadType.OPTIONAL => Ar.ReadFlag(),
            ReadType.ZERO => tagData?.Bool == true,
            ReadType.RAW => Ar.ReadBoolean(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
