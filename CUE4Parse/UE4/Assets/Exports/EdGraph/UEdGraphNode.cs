using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.EdGraph;

public class UEdGraphNode : UObject
{
    public UEdGraphPinReference?[] Pins = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (!Ar.IsFilterEditorOnly && FBlueprintsObjectVersion.Get(Ar) >= FBlueprintsObjectVersion.Type.EdGraphPinOptimized)
        {
            UEdGraphPin.SerializeAsOwningNode(Ar, ref Pins);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Pins));
        serializer.Serialize(writer, Pins);
    }
}
