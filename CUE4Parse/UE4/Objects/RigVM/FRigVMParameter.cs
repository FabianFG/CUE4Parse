using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMParameter
{
    public ERigVMParameterType Type;
    public FName Name;
    public int RegisterIndex;
    public string CPPType;
    public FPackageIndex? ScriptStruct; // UScriptStruct
    public FName ScriptStructPath; // UScriptStruct

    public FRigVMParameter(FAssetArchive Ar)
    {
        if (FAnimObjectVersion.Get(Ar) < FAnimObjectVersion.Type.StoreMarkerNamesOnSkeleton) return;

        Type = Ar.Read<ERigVMParameterType>();
        Name = Ar.ReadFName();
        RegisterIndex = Ar.Read<int>();
        CPPType = Ar.ReadFString();
        ScriptStructPath = Ar.ReadFName();
    }
}

public enum ERigVMParameterType : byte
{
    Input,
    Output,
    Invalid
}
