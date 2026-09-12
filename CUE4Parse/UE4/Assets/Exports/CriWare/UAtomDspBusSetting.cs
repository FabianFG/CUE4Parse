using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CriWare;

public class UAtomDspBusSetting : UObject
{
    public FName[][] BusEffectNames = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        BusEffectNames = Ar.ReadArray(() => Ar.ReadArray(Ar.ReadFName));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(BusEffectNames));
        serializer.Serialize(writer, BusEffectNames);
    }
}
