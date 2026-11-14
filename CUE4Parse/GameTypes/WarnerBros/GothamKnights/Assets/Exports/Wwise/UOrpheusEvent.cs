using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.WarnerBros.GothamKnights.Assets.Exports.Wwise;

public class UOrpheusEvent : UObject
{
    public uint AudioEventId;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AudioEventId = Ar.Read<uint>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(AudioEventId));
        writer.WriteValue(AudioEventId);
    }
}
