using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UCustomizableObject : UObject
{
    public long InternalVersion;
    public FModel? Model;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        // tested only on 5.7+, but in theory should also work on 5.6
        if (Ar.Game < Versions.EGame.GAME_UE5_6) return;
        InternalVersion = Ar.Read<long>();
        if (InternalVersion != -1)
            Model = new FModel(new FMutableArchive(Ar));
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
