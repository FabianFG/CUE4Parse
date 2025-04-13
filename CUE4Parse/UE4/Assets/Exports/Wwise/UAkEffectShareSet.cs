using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkEffectShareSet : UAkAudioType
{
    public FStructFallback? ShareSetCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        ShareSetCookedData = new FStructFallback(Ar, "WwiseLocalizedShareSetCookedData");
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (ShareSetCookedData is null) return;

        writer.WritePropertyName("ShareSetCookedData");
        serializer.Serialize(writer, ShareSetCookedData);
    }
}
