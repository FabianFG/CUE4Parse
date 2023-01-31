using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports;

public class UObjectRedirector : UObject
{
    public FPackageIndex? DestinationObject;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DestinationObject = new FPackageIndex(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName("DestinationObject");
        serializer.Serialize(writer, DestinationObject);
    }
}
