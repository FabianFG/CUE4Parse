using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMGraphFunctionIdentifier
{
    public string LibraryNodePath;
    public FSoftObjectPath HostObject;

    public FRigVMGraphFunctionIdentifier(FAssetArchive Ar)
    {
        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.RemoveLibraryNodeReferenceFromFunctionIdentifier)
        {
            LibraryNodePath = new FSoftObjectPath(Ar).ToString();
        }
        else
        {
            LibraryNodePath = Ar.ReadFString();
        }
        HostObject = new FSoftObjectPath(Ar);
    }
}
