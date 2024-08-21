using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.RigVM;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Engine;

public class UControlRigBlueprintGeneratedClass : URigVMBlueprintGeneratedClass
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        BlueprintDeserialize(Ar, validPos);
        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.SwitchedToRigVM) return;
        VM = new URigVM();
        VM.Deserialize(Ar, validPos);
        if (FControlRigObjectVersion.Get(Ar) < FControlRigObjectVersion.Type.StoreFunctionsInGeneratedClass) return;
        GraphFunctionStore = new FRigVMGraphFunctionStore(Ar);
    }
}
