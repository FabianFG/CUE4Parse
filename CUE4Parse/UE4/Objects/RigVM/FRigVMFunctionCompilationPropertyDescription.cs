using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMFunctionCompilationPropertyDescription
{
    public FName Name;
    public string CPPType;
    public FSoftObjectPath CPPTypeObject;//TSoftObjectPtr<UObject> CPPTypeObject;
    public string DefaultValue;

    public FRigVMFunctionCompilationPropertyDescription(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        CPPType = Ar.ReadFString();
        CPPTypeObject = new FSoftObjectPath(Ar);
        DefaultValue = Ar.ReadFString();
    }
}
