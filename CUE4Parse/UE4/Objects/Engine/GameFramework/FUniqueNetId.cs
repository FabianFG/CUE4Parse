using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Engine.GameFramework;

public class FUniqueNetId : ISerializable
{
    public FName Type;
    public string Contents;

    public FUniqueNetId(FName type, string contents)
    {
        Type = type;
        Contents = contents;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Serialize(Type);
        // TODO: FString   
        // Ar.WriteFString(Contents);
    }
}