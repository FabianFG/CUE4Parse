using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

public class Utf8StrProperty : StrProperty
{
    public Utf8StrProperty() => Value = string.Empty;
    public Utf8StrProperty(string value) => Value = value;
    public Utf8StrProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Ar.ReadFUtf8String()
        };
    }
}
