using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(OptionalPropertyConverter))]
public class OptionalProperty : FPropertyTagType<FPropertyTagType>
{
    public OptionalProperty(FPropertyTagType value) => Value = value;

    public OptionalProperty(FAssetArchive Ar, FPropertyTagData? tagData, ReadType type)
    {
        if (tagData == null)
            throw new ParserException(Ar, "Can't load OptionalProperty without tag data");
        if (tagData.InnerType == null)
            throw new ParserException(Ar, "OptionalProperty needs inner type");

        if (type == ReadType.ZERO || !Ar.ReadBoolean())
        {
            Value = default;
            return;
        }

        Value = ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.OPTIONAL) ?? default;
    }
}
