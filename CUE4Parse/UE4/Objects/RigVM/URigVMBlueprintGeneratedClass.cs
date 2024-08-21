using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public class URigVMBlueprintGeneratedClass : UBlueprintGeneratedClass
{
    public URigVM VM;
    public FRigVMGraphFunctionStore GraphFunctionStore;

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
}
