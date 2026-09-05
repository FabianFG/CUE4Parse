using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

public class UPhysicsAssetInstance : Assets.Exports.UObject
{
    public Dictionary<FRigidBodyIndexPair, bool> CollisionDisableTable;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        CollisionDisableTable = Ar.ReadMap(() => new FRigidBodyIndexPair(Ar), Ar.ReadBoolean);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("CollisionDisableTable");
        writer.WriteStartArray();

        foreach (var Table in CollisionDisableTable)
        {
            serializer.Serialize(writer, Table);
        }

        writer.WriteEndArray();
    }
}
