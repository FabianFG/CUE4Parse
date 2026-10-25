using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Verse;

public interface ISolarisDebugData;

public class UVerseDebugData : UObject
{
    public ISolarisDebugData? DebugData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var payloadValue = Ar.Read<EVerseDebugDataPayload>();
        DebugData = payloadValue switch
        {
            EVerseDebugDataPayload.Package => new FSolarisPackageDebugData(Ar),
            EVerseDebugDataPayload.Compact => new FSolarisClientDebugData(Ar),
            _ => null,
        };
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(DebugData));
        serializer.Serialize(writer, DebugData);
    }

    public enum EVerseDebugDataPayload
    {
        None = 0,
        Package = 1,
        Compact = 2,
    };
}
