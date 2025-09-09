using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkRtpc : UAkAudioType
{
    public FStructFallback? GameParameterCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        GameParameterCookedData = new FStructFallback(Ar, "WwiseGameParameterCookedData");
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (GameParameterCookedData is null) return;

        writer.WritePropertyName("GameParameterCookedData");
        serializer.Serialize(writer, GameParameterCookedData);
    }
}
