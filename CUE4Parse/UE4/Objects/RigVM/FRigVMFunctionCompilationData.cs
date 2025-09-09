using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMFunctionCompilationData
{
    public FRigVMByteCode ByteCode;
    public FName[] FunctionNames;
    public FRigVMFunctionCompilationPropertyDescription[] WorkPropertyDescriptions;
    public FRigVMFunctionCompilationPropertyPath[] WorkPropertyPathDescriptions;
    public FRigVMFunctionCompilationPropertyDescription[] LiteralPropertyDescriptions;
    public FRigVMFunctionCompilationPropertyPath[] LiteralPropertyPathDescriptions;
    public FRigVMFunctionCompilationPropertyDescription[] DebugPropertyDescriptions;
    public FRigVMFunctionCompilationPropertyPath[] DebugPropertyPathDescriptions;
    public FRigVMFunctionCompilationPropertyDescription[] ExternalPropertyDescriptions;
    public FRigVMFunctionCompilationPropertyPath[] ExternalPropertyPathDescriptions;
    public Dictionary<int, FName> ExternalRegisterIndexToVariable;
    public Dictionary<string, FRigVMOperand> Operands;
    public uint Hash;
    public bool bEncounteredSurpressedErrors;
    public Dictionary<FRigVMOperand, FRigVMOperand[]>? OperandToDebugRegisters;

    public FRigVMFunctionCompilationData(FAssetArchive Ar)
    {
        ByteCode = new FRigVMByteCode(Ar);
        FunctionNames = Ar.ReadArray(Ar.ReadFName);
        WorkPropertyDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyDescription(Ar));
        WorkPropertyPathDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyPath(Ar));
        LiteralPropertyDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyDescription(Ar));
        LiteralPropertyPathDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyPath(Ar));
        DebugPropertyDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyDescription(Ar));
        DebugPropertyPathDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyPath(Ar));
        ExternalPropertyDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyDescription(Ar));
        ExternalPropertyPathDescriptions = Ar.ReadArray(() => new FRigVMFunctionCompilationPropertyPath(Ar));

        ExternalRegisterIndexToVariable = Ar.ReadMap(Ar.Read<int>, Ar.ReadFName);
        Operands = Ar.ReadMap(Ar.ReadFString, Ar.Read<FRigVMOperand>);
        Hash = Ar.Read<uint>();
        bEncounteredSurpressedErrors = false;

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData &&
            FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData)
            return;

        OperandToDebugRegisters = Ar.ReadMap(Ar.Read<byte>(), Ar.Read<FRigVMOperand>, () => Ar.ReadArray(Ar.Read<byte>(), Ar.Read<FRigVMOperand>));
    }
}
