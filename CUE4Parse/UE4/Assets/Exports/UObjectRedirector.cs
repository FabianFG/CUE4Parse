using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
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

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);

        Ar.Serialize(DestinationObject);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("DestinationObject");
        serializer.Serialize(writer, DestinationObject);
    }
}
