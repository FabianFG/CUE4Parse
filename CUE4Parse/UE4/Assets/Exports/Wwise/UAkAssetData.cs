using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAssetData : UObject
{
    public WwiseReader? Data;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bulkData = new FByteBulkData(Ar);
        if (bulkData.Data is null) return;

        var reader = new FByteArchive("AkAssetData", bulkData.Data, Ar.Versions);
        Data = new WwiseReader(reader);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (Data is null) return;

        writer.WritePropertyName("Data");
        serializer.Serialize(writer, Data);
    }
}
