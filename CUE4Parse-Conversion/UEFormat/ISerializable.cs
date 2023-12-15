using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.UEFormat;

public interface ISerializable
{
    public void Serialize(FArchiveWriter Ar);
}