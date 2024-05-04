using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.UEFormat.Structs;

public class FStaticDataChunk : FArchiveWriter, ISerializable
{
    private readonly FString HeaderName;
    
    public FStaticDataChunk(string headerName)
    {
        HeaderName = new FString(headerName);
    }

    public void Serialize(FArchiveWriter Ar)
    {
        HeaderName.Serialize(Ar);
        Ar.Write((int) Length);
        Ar.Write(GetBuffer());
    }
}