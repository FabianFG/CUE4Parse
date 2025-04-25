using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMGraphFunctionData
{
    public FRigVMGraphFunctionHeader Header;
    public FRigVMFunctionCompilationData CompilationData;
    public string? SerializedCollapsedNode;
    public FRigVMObjectArchive? CollapseNodeArchive;

    public FRigVMGraphFunctionData(FAssetArchive Ar)
    {
        Header = new FRigVMGraphFunctionHeader(Ar);
        CompilationData = new FRigVMFunctionCompilationData(Ar);

        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RigVMSaveSerializedGraphInGraphFunctionData)
            return;

        SerializedCollapsedNode = Ar.ReadFString();

        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.RigVMSaveSerializedGraphInGraphFunctionDataAsByteArray)
            return;

        CollapseNodeArchive = new FRigVMObjectArchive(Ar);
    }
}


public struct FRigVMGraphFunctionStore(FAssetArchive Ar)
{
    public FRigVMGraphFunctionData[] PublicFunctions = Ar.ReadArray(() => new FRigVMGraphFunctionData(Ar));
}
