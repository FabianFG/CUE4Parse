using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMByteCode
{
    public List<IRigInstruction> Instructions = [];
    public string[] Entries = [];
    public FRigVMBranchInfo[] BranchInfos = [];
    public string? PublicContextPathName;
    public bool bHasPublicContextPathName = false;

    public FRigVMByteCode(FAssetArchive Ar)
    {
        if (FAnimObjectVersion.Get(Ar) < FAnimObjectVersion.Type.StoreMarkerNamesOnSkeleton)
        {
            var size = Ar.Read<int>();
            using var RigVMAr = new FByteArchive("ByteCode", Ar.ReadBytes(size), Ar.Versions);

            try
            {
                while (RigVMAr.Position < RigVMAr.Length)
                {
                    Instructions.Add(ReadRigVMInstruction(RigVMAr));
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Failed to serialize RigVM bytecode");
            }

            return;
        }

        var instructionCount = Ar.Read<int>();
        for (var i = 0; i < instructionCount; i++)
        {
            Instructions.Add(ReadRigVMInstruction(Ar));
        }

        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.SerializeRigVMEntries)
        {
            Entries = Ar.ReadArray(Ar.ReadFString);
        }

        if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.RigVMLazyEvaluation)
        {
            BranchInfos = Ar.ReadArray(() => new FRigVMBranchInfo(Ar));
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.VMBytecodeStorePublicContextPath)
        {
            PublicContextPathName = Ar.ReadFString();
            bHasPublicContextPathName = true;
        }
    }

    public IRigInstruction ReadRigVMInstruction(FArchive Ar)
    {
        var opCode = Ar.Read<ERigVMOpCode>();
        IRigInstruction op = opCode switch
        {
            <= ERigVMOpCode.Execute_64_Operands or ERigVMOpCode.Execute => new FRigVMExecuteOp(Ar),
            ERigVMOpCode.Copy => new FRigVMCopyOp(Ar),
            ERigVMOpCode.Zero or ERigVMOpCode.BoolFalse or ERigVMOpCode.BoolTrue or ERigVMOpCode.Increment
                or ERigVMOpCode.Decrement or ERigVMOpCode.ArrayReset or ERigVMOpCode.ArrayReverse=> Ar.Read<FRigVMUnaryOp>(),
            ERigVMOpCode.Equals or ERigVMOpCode.NotEquals => Ar.Read<FRigVMComparisonOp>(),
            ERigVMOpCode.JumpAbsolute or ERigVMOpCode.JumpForward or ERigVMOpCode.JumpBackward => Ar.Read<FRigVMJumpOp>(),
            ERigVMOpCode.JumpAbsoluteIf or ERigVMOpCode.JumpForwardIf or ERigVMOpCode.JumpBackwardIf => new FRigVMJumpIfOp(Ar),
            ERigVMOpCode.BeginBlock or ERigVMOpCode.ArrayGetNum or ERigVMOpCode.ArraySetNum or ERigVMOpCode.ArrayAppend
                or ERigVMOpCode.ArrayClone or ERigVMOpCode.ArrayRemove or ERigVMOpCode.ArrayUnion => Ar.Read<FRigVMBinaryOp>(),
            ERigVMOpCode.ArrayAdd or ERigVMOpCode.ArrayGetAtIndex or ERigVMOpCode.ArraySetAtIndex or ERigVMOpCode.ArrayInsert
                or ERigVMOpCode.ArrayDifference or ERigVMOpCode.ArrayIntersection => Ar.Read<FRigVMTernaryOp>(),
            ERigVMOpCode.ArrayFind => Ar.Read<FRigVMQuaternaryOp>(),
            ERigVMOpCode.ArrayIterator => Ar.Read<FRigVMSenaryOp>(),
            ERigVMOpCode.InvokeEntry => Ar.Read<FRigVMInvokeEntryOp>(),
            ERigVMOpCode.JumpToBranch => Ar.Read<FRigVMJumpToBranchOp>(),
            ERigVMOpCode.RunInstructions => Ar.Read<FRigVMRunInstructionsOp>(),
            _ => new FRigVMBaseOp(opCode),
        };
        return op;
    }
}

public readonly struct FRigVMBranchInfo(FAssetArchive Ar)
{
    public readonly int Index = Ar.Read<int>();
    public readonly FName Label = Ar.ReadFString();
    public readonly int InstructionIndex = Ar.Read<int>();
    public readonly int ArgumentIndex = Ar.Read<int>();
    public readonly ushort FirstInstruction = Ar.Read<ushort>();
    public readonly ushort LastInstruction = Ar.Read<ushort>();
}

public interface IRigInstruction;

public readonly struct FRigVMBaseOp(ERigVMOpCode opCode) : IRigInstruction
{
    public readonly ERigVMOpCode OpCode = opCode;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FRigVMExecuteOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly ushort FunctionIndex;
    public readonly ushort ArgumentCount;
    public readonly ushort FirstPredicateIndex;
    public readonly ushort PredicateCount;
    public readonly FRigVMOperand[] Arguments;

    public FRigVMExecuteOp(FArchive Ar)
    {
        OpCode = Ar.Read<ERigVMOpCode>();
        FunctionIndex = Ar.Read<ushort>();

        if (OpCode >= ERigVMOpCode.Execute_0_Operands && OpCode <= ERigVMOpCode.Execute_64_Operands)
        {
            ArgumentCount = (OpCode - ERigVMOpCode.Execute_0_Operands);
            OpCode = ERigVMOpCode.Execute;
        }
        else
        {
            ArgumentCount = Ar.Read<ushort>();
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.PredicatesAddedToExecuteOps)
        {
            FirstPredicateIndex = Ar.Read<ushort>();
            PredicateCount = Ar.Read<ushort>();
        }

        Arguments = Ar.ReadArray<FRigVMOperand>(ArgumentCount);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 5)]
public readonly struct FRigVMOperand
{
    public readonly ERigVMMemoryType MemoryType;
    public readonly ushort RegisterIndex;
    public readonly ushort RegisterOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 6)]
public readonly struct FRigVMUnaryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand Arg;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 11)]
public readonly struct FRigVMBinaryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand ArgA;
    public readonly FRigVMOperand ArgB;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
public readonly struct FRigVMTernaryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand ArgA;
    public readonly FRigVMOperand ArgB;
    public readonly FRigVMOperand ArgC;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 21)]
public readonly struct FRigVMQuaternaryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand ArgA;
    public readonly FRigVMOperand ArgB;
    public readonly FRigVMOperand ArgC;
    public readonly FRigVMOperand ArgD;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 26)]
public readonly struct FRigVMQuinaryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand ArgA;
    public readonly FRigVMOperand ArgB;
    public readonly FRigVMOperand ArgC;
    public readonly FRigVMOperand ArgD;
    public readonly FRigVMOperand ArgE;
}

[StructLayout(LayoutKind.Sequential, Pack = 1,Size = 31)]
public readonly struct FRigVMSenaryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand ArgA;
    public readonly FRigVMOperand ArgB;
    public readonly FRigVMOperand ArgC;
    public readonly FRigVMOperand ArgD;
    public readonly FRigVMOperand ArgE;
    public readonly FRigVMOperand ArgF;
}

public enum ERigVMCopyType : byte
{
    Default,
    FloatToDouble,
    DoubleToFloat
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FRigVMCopyOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand Source;
    public readonly FRigVMOperand Target;
    public readonly ushort NumBytes;
    public readonly ERigVMRegisterType RegisterType;
    public readonly ERigVMCopyType CopyType;

    public FRigVMCopyOp(FArchive Ar)
    {
        OpCode = Ar.Read<ERigVMOpCode>();
        Source = Ar.Read<FRigVMOperand>();
        Target = Ar.Read<FRigVMOperand>();

        if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RigVMCopyOpStoreNumBytes)
        {
            NumBytes = 0;
            RegisterType = ERigVMRegisterType.Invalid;
        }
        else
        {
            NumBytes = Ar.Read<ushort>();
            RegisterType = Ar.Read<ERigVMRegisterType>();
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
public readonly struct FRigVMComparisonOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand A;
    public readonly FRigVMOperand B;
    public readonly FRigVMOperand Result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 5)]
public readonly struct FRigVMJumpOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly int InstructionIndex;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FRigVMJumpIfOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand Arg;
    public readonly int InstructionIndex;
    public readonly bool Condition;

    public FRigVMJumpIfOp(FArchive Ar)
    {
        OpCode = Ar.Read<ERigVMOpCode>();
        Arg = Ar.Read<FRigVMOperand>();
        InstructionIndex = Ar.Read<int>();
        Condition = Ar.ReadBoolean();
    }
}

public readonly struct FRigVMInvokeEntryOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FName EntryName;

    public FRigVMInvokeEntryOp(FArchive Ar)
    {
        OpCode = ERigVMOpCode.InvokeEntry;
        EntryName = Ar.ReadFString();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)]
public readonly struct FRigVMJumpToBranchOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand Arg;
    public readonly int FirstBranchInfoIndex;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14)]
public readonly struct FRigVMRunInstructionsOp : IRigInstruction
{
    public readonly ERigVMOpCode OpCode;
    public readonly FRigVMOperand Arg;
    public readonly int StartInstruction;
    public readonly int EndInstruction;
}
