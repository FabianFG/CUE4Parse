using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(OptionalPropertyConverter))]
public class OptionalProperty : FPropertyTagType<FPropertyTagType>
{
    public OptionalProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
    {
        if (tagData == null)
            throw new ParserException(Ar, "Can't load OptionalProperty without tag data");
        if (tagData.InnerType == null)
            throw new ParserException(Ar, "OptionalProperty needs inner type");

        if (!Ar.ReadBoolean())
        {
            Value = default;
            return;
        }

        Value = type switch
        {
            ReadType.ZERO => default,
            _ => ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, type) ?? default
        };
    }
}
