using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Writers.UEFormat.Structs;

public class FDataAttribute : FArchiveWriter, ISerializable
{
    private readonly FString Name;

    public FDataAttribute(string name)
    {
        Name = new FString(name);
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Name.Serialize(Ar);
        Ar.Write((int) Length);
        Ar.Write(GetBuffer());
    }
}
