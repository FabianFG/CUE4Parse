namespace CUE4Parse.UE4.Objects.RigVM;

public enum ERigVMRegisterType : byte
{
    Plain, // bool, int32, float, FVector etc.
    String, // FString
    Name, // FName
    Struct, // Any USTRUCT
    Invalid
}
