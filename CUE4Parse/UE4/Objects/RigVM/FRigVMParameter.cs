using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.RigVM
{
    public class FRigVMParameter
    {
        public ERigVMParameterType Type;
        public FName Name;
        public int RegisterIndex;
        public string CPPType;
        public FPackageIndex ScriptStruct; // UScriptStruct

        public FRigVMParameter(FAssetArchive Ar)
        {
            Type = Ar.Read<ERigVMParameterType>();
            Name = Ar.ReadFName();
            RegisterIndex = Ar.Read<int>();
            CPPType = Ar.ReadFString();
            ScriptStruct = new FPackageIndex(Ar);
        }
    }

    public enum ERigVMParameterType : byte
    {
        Input,
        Output,
        Invalid
    }
}