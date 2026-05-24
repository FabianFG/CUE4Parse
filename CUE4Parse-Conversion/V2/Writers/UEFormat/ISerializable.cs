using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.UEFormat;

public interface ISerializable
{
    public void Serialize(FArchiveWriter Ar);
}
