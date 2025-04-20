using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkInitBank : UAkAudioType
{
    public FStructFallback? InitBankCookedData { get; private set; }
    public WwiseReader? Data;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        InitBankCookedData = new FStructFallback(Ar, "WwiseInitBankCookedData");

        // can be inlined, not sure how to check
        if (Ar.Position >= validPos) return;

        var bulkData = new FByteBulkData(Ar);
        if (bulkData.Data is null) return;

        var reader = new FByteArchive("AkAssetData", bulkData.Data, Ar.Versions);
        Data = new WwiseReader(reader);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (InitBankCookedData is null) return;

        writer.WritePropertyName("InitBankCookedData");
        serializer.Serialize(writer, InitBankCookedData);

        if (Data is null) return;

        writer.WritePropertyName("Data");
        serializer.Serialize(writer, Data);
    }
}
