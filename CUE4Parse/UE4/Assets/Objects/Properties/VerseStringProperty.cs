using System.Text;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

[JsonConverter(typeof(VerseStringPropertyConverter))]
public class VerseStringProperty : FPropertyTagType<string>
{
    public VerseStringProperty(string value)
    {
        Value = value;
    }

    public VerseStringProperty(FArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => ReadString(Ar)
        };
    }

    private string ReadString(FArchive Ar)
    {
        var len = Ar.Read<int>();
        if (len < 0)
            throw new ParserException("VerseStringProperty negative length string");
        var text = Ar.ReadBytes(len);
        return Encoding.ASCII.GetString(text);
    }
}
