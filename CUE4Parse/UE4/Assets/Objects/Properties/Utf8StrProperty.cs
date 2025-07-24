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
            _ => ReadUtf8String(Ar)
        };
    }
    
    public string ReadUtf8String(FArchive Ar)
    {
        int length = Ar.Read<int>();
        if (length < 0)
            throw new ParserException($"Negative Utf8String length '{length}'");

        if (length > Ar.Length - Ar.Position)
        {
            throw new ParserException($"Invalid Utf8String length '{length}'");
        }
            
        byte[] bytes = Ar.ReadBytes(length);

        return Encoding.UTF8.GetString(bytes);
    }

}