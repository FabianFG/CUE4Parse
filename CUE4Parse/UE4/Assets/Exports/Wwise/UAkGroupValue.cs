using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkGroupValue : UAkAudioType
{
    public FStructFallback GroupValueCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        GroupValueCookedData = new FStructFallback(Ar, "WwiseGroupValueCookedData");
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("GroupValueCookedData");
        serializer.Serialize(writer, GroupValueCookedData);
    }
}
