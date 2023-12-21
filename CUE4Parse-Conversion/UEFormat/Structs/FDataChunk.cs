using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.UEFormat.Structs;

public class FDataChunk : FArchiveWriter, ISerializable
{
    public int Count;
    private readonly FString HeaderName;
    
    public FDataChunk(string headerName, int count)
    {
        HeaderName = new FString(headerName);
        Count = count;
    }
    
    public FDataChunk(string headerName)
    {
        HeaderName = new FString(headerName);
        Count = 0;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        HeaderName.Serialize(Ar);
        Ar.Write(Count);
        Ar.Write((int) Length);
        Ar.Write(GetBuffer());
    }
}