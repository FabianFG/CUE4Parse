using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAcousticTexture : UAkAudioType
{
    public FStructFallback? AcousticTextureCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos)
            return;

        AcousticTextureCookedData = new FStructFallback(Ar, "WwiseAcousticTextureCookedData");
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (AcousticTextureCookedData is null) return;

        writer.WritePropertyName("AcousticTextureCookedData");
        serializer.Serialize(writer, AcousticTextureCookedData);
    }
}
