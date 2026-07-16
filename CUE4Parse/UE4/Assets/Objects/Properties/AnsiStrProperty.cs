using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

public class AnsiStrProperty : StrProperty
{
    public AnsiStrProperty() => Value = string.Empty;
    public AnsiStrProperty(string value) => Value = value;
    public AnsiStrProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Ar.ReadFAnsiString()
        };
    }
}
