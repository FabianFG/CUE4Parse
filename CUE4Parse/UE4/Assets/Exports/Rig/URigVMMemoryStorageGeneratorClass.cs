using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.RigVM;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class URigVMMemoryStorageGeneratorClass : UClass
{
    public ERigVMMemoryType MemoryType;
    // A list of descriptions for the property paths - used for serialization
    public FRigVMPropertyPathDescription[] PropertyPathDescriptions;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        PropertyPathDescriptions = Ar.ReadArray(() => new FRigVMPropertyPathDescription(Ar));
        MemoryType = Ar.Read<ERigVMMemoryType>();
    }
}
