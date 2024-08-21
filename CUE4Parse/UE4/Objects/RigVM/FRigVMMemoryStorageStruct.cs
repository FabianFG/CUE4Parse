using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.StructUtils;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMPropertyPathDescription(FAssetArchive Ar)
{
    public int PropertyIndex = Ar.Read<int>();
    public string HeadCPPType = Ar.ReadFString();
    public string SegmentPath = Ar.ReadFString();
}

public class FRigVMMemoryStorageStruct : FInstancedPropertyBag
{
    public ERigVMMemoryType MemoryType;
    public FRigVMPropertyPathDescription[] PropertyPathDescriptions;

    public FRigVMMemoryStorageStruct(FAssetArchive Ar) : base(Ar)
    {
        MemoryType = Ar.Read<ERigVMMemoryType>();
        PropertyPathDescriptions = Ar.ReadArray(() => new FRigVMPropertyPathDescription(Ar));
    }
}
