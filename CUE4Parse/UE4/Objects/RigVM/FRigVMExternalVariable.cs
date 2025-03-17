using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMExternalVariable
{
    public FName Name;
    public FName TypeName;
    public FSoftObjectPath TypeObjectPath;
    public bool bIsArray;
    public bool bIsPublic;
    public bool bIsReadOnly;
    public int Size;

    public FRigVMExternalVariable(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        TypeName = Ar.ReadFName();
        TypeObjectPath = new FSoftObjectPath(Ar);
        bIsArray = Ar.ReadBoolean();
        bIsPublic = Ar.ReadBoolean();
        bIsReadOnly = Ar.ReadBoolean();
        Size = Ar.Read<int>();
    }
}
