using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkInitBank : UAkAudioType
{
    public FWwiseInitBankCookedData? InitBankCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        InitBankCookedData = new FWwiseInitBankCookedData(new FStructFallback(Ar, "WwiseInitBankCookedData"));
        InitBankCookedData.SerializeBulkData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (InitBankCookedData is null) return;

        writer.WritePropertyName(nameof(InitBankCookedData));
        serializer.Serialize(writer, InitBankCookedData);
    }
}
