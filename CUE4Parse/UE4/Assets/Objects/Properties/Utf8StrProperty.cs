using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(Utf8StrPropertyConverter))]
public class Utf8StrProperty : FPropertyTagType<string>
{
    public Utf8StrProperty(string value)
    {
        Value = value;
    }

    public Utf8StrProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Ar.ReadFUtf8String()
        };
    }
}
