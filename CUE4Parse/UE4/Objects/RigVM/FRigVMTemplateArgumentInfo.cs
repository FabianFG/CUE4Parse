using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct TRigVMTypeIndex(FAssetArchive Ar)
{
    public FName CPPType = Ar.ReadFName();
    public FPackageIndex CPPTypeObject = new FPackageIndex(Ar);
}

public class FRigVMTemplateArgumentInfo
{
    public FName ArgumentName;
    public ERigVMFunctionArgumentDirection Direction;
    public TRigVMTypeIndex[] TypeIndices;
    public FRigVMTemplateArgumentInfo(FAssetArchive Ar)
    {
        ArgumentName = Ar.ReadFName();
        Direction = Ar.Read<ERigVMFunctionArgumentDirection>();
        TypeIndices = Ar.ReadArray(() => new TRigVMTypeIndex(Ar));
    }
}
