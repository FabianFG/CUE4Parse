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
        var savedPosition = Ar.Position;
        if (bulkData.Data is null) return;

        using var reader = new FByteArchive("AkAssetData", bulkData.Data, Ar.Versions);
        Data = new WwiseReader(reader);

        if (bulkData.BulkDataFlags is EBulkDataFlags.BULKDATA_None)
        {
            Ar.Position = savedPosition + bulkData.Header.ElementCount;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (Data is null) return;

        writer.WritePropertyName("Data");
        serializer.Serialize(writer, Data);
    }
}
