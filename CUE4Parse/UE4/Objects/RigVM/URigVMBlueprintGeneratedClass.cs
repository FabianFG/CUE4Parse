using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.RigVM;

public class URigVMBlueprintGeneratedClass : UBlueprintGeneratedClass
{
    public URigVM? VM;
    public FRigVMGraphFunctionStore? GraphFunctionStore;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RigVMGeneratedClass) return;
        VM = new URigVM();
        VM.Deserialize(Ar, validPos);

        GraphFunctionStore = new FRigVMGraphFunctionStore(Ar);
    }

    protected void BlueprintDeserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (VM != null)
        {
            writer.WritePropertyName(nameof(VM));
            serializer.Serialize(writer, VM);
        }

        if (GraphFunctionStore != null)
        {
            writer.WritePropertyName(nameof(GraphFunctionStore));
            serializer.Serialize(writer, GraphFunctionStore);
        }

    }
}
