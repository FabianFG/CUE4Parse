using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Actor;

public class ALevelInstance : AActor
{
    public FGuid LevelInstanceActorGuid;
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Position >= validPos) return;
        LevelInstanceActorGuid = Ar.Read<FGuid>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(LevelInstanceActorGuid));
        serializer.Serialize(writer, LevelInstanceActorGuid);
    }
}
