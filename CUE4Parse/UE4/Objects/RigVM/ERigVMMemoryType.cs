namespace CUE4Parse.UE4.Objects.RigVM;

public enum ERigVMMemoryType : byte
{
    Work, // Mutable state
    Literal, // Const / fixed state
    External, // Unowned external memory
    Debug,
    Invalid
}
