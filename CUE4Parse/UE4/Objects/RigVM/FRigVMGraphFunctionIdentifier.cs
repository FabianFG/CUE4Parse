using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMGraphFunctionIdentifier(FAssetArchive Ar)
{
    public FSoftObjectPath LibraryNode = new FSoftObjectPath(Ar);
    public FSoftObjectPath HostObject = new FSoftObjectPath(Ar);
}
