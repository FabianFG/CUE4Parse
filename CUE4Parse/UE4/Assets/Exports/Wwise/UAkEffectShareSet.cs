using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkEffectShareSet : UAkAudioType
{
    public FWwiseLocalizedShareSetCookedData? ShareSetCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        ShareSetCookedData = new FWwiseLocalizedShareSetCookedData(new FStructFallback(Ar, "WwiseLocalizedShareSetCookedData"));
        ShareSetCookedData?.SerializeBulkData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (ShareSetCookedData is null) return;

        writer.WritePropertyName(nameof(ShareSetCookedData));
        serializer.Serialize(writer, ShareSetCookedData);
    }
}
