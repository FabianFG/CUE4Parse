using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAuxBus : UAkAudioType
{
    public FStructFallback? AuxBusCookedData { get; private set; }
    public float MaxAttenuationRadius { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        AuxBusCookedData = new FStructFallback(Ar, "WwiseLocalizedAuxBusCookedData");
        MaxAttenuationRadius = Ar.Read<float>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (AuxBusCookedData is null) return;

        writer.WritePropertyName("AuxBusCookedData");
        serializer.Serialize(writer, AuxBusCookedData);

        writer.WritePropertyName("MaxAttenuationRadius");
        writer.WriteValue(MaxAttenuationRadius);
    }
}
