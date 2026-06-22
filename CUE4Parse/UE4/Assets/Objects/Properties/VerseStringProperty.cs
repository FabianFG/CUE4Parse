using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

public class VerseStringProperty : StrProperty
{
    public VerseStringProperty() => Value = string.Empty;
    public VerseStringProperty(string value) => Value = value;
    public VerseStringProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Ar.ReadFUtf8String()
        };
    }
}
