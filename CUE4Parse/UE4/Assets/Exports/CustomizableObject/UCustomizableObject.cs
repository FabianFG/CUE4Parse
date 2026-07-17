using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UCustomizableObject : UObject
{
    public long InternalVersion;
    public FModel? Model;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        InternalVersion = Ar.Game >= GAME_UE5_6 ? Ar.Read<long>() : Ar.Read<int>();
        if (InternalVersion != -1)
            Model = new FModel(new FMutableArchive(Ar));

        if (Ar.Game is GAME_LordsoftheFallen)
        {
             CustomGameData = new FByteBulkData(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(InternalVersion));
        writer.WriteValue(InternalVersion);
        writer.WritePropertyName(nameof(Model));
        serializer.Serialize(writer, Model);
    }
}
