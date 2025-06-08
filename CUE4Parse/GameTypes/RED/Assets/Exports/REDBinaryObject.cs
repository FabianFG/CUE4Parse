using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.RED.Assets.Exports;

public class UREDBinaryObject : UObject
{
    public FByteBulkData DataBE;
    public FByteBulkData DataLE;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        DataBE = new FByteBulkData(Ar);
        DataLE = new FByteBulkData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("DataBE");
        serializer.Serialize(writer, DataBE);

        writer.WritePropertyName("DataLE");
        serializer.Serialize(writer, DataLE);
    }
}