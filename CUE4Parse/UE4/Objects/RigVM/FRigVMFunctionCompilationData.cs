using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.RigVM;

public struct FRigVMFunctionCompilationData
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
    public Dictionary<FRigVMOperand, FRigVMOperand[]> OperandToDebugRegisters;

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

        var num = Ar.Read<int>();
        ExternalRegisterIndexToVariable = [];
        for (var i = 0; i < num; i++)
        {
            ExternalRegisterIndexToVariable[Ar.Read<int>()] = Ar.ReadFName();
        }

        num = Ar.Read<int>();
        Operands = [];
        for (var i = 0; i < num; i++)
        {
            Operands[Ar.ReadFString()] = Ar.Read<FRigVMOperand>();
        }

        Hash = Ar.Read<uint>();
        bEncounteredSurpressedErrors = false;

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) < FUE5ReleaseStreamObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData &&
            FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData)
            return;


        num = Ar.Read<byte>();
        OperandToDebugRegisters = [];
        for (var i = 0; i < num; i++)
        {
            OperandToDebugRegisters[Ar.Read<FRigVMOperand>()] = Ar.ReadArray(Ar.Read<byte>(),Ar.Read<FRigVMOperand>);
        }
    }
}
