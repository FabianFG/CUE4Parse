using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.RigVM
{
    public class FRigVMByteCode
    {
        public readonly int InstructionCount;
        public readonly ERigVMOpCode OpCode;

        public FRigVMByteCode(FArchive Ar)
        {
            InstructionCount = Ar.Read<int>();
            OpCode = Ar.Read<ERigVMOpCode>();

            switch (OpCode)
            {
                case ERigVMOpCode.Execute_0_Operands:
                case ERigVMOpCode.Execute_1_Operands:
                case ERigVMOpCode.Execute_2_Operands:
                case ERigVMOpCode.Execute_3_Operands:
                case ERigVMOpCode.Execute_4_Operands:
                case ERigVMOpCode.Execute_5_Operands:
                case ERigVMOpCode.Execute_6_Operands:
                case ERigVMOpCode.Execute_7_Operands:
                case ERigVMOpCode.Execute_8_Operands:
                case ERigVMOpCode.Execute_9_Operands:
                case ERigVMOpCode.Execute_10_Operands:
                case ERigVMOpCode.Execute_11_Operands:
                case ERigVMOpCode.Execute_12_Operands:
                case ERigVMOpCode.Execute_13_Operands:
                case ERigVMOpCode.Execute_14_Operands:
                case ERigVMOpCode.Execute_15_Operands:
                case ERigVMOpCode.Execute_16_Operands:
                case ERigVMOpCode.Execute_17_Operands:
                case ERigVMOpCode.Execute_18_Operands:
                case ERigVMOpCode.Execute_19_Operands:
                case ERigVMOpCode.Execute_20_Operands:
                case ERigVMOpCode.Execute_21_Operands:
                case ERigVMOpCode.Execute_22_Operands:
                case ERigVMOpCode.Execute_23_Operands:
                case ERigVMOpCode.Execute_24_Operands:
                case ERigVMOpCode.Execute_25_Operands:
                case ERigVMOpCode.Execute_26_Operands:
                case ERigVMOpCode.Execute_27_Operands:
                case ERigVMOpCode.Execute_28_Operands:
                case ERigVMOpCode.Execute_29_Operands:
                case ERigVMOpCode.Execute_30_Operands:
                case ERigVMOpCode.Execute_31_Operands:
                case ERigVMOpCode.Execute_32_Operands:
                case ERigVMOpCode.Execute_33_Operands:
                case ERigVMOpCode.Execute_34_Operands:
                case ERigVMOpCode.Execute_35_Operands:
                case ERigVMOpCode.Execute_36_Operands:
                case ERigVMOpCode.Execute_37_Operands:
                case ERigVMOpCode.Execute_38_Operands:
                case ERigVMOpCode.Execute_39_Operands:
                case ERigVMOpCode.Execute_40_Operands:
                case ERigVMOpCode.Execute_41_Operands:
                case ERigVMOpCode.Execute_42_Operands:
                case ERigVMOpCode.Execute_43_Operands:
                case ERigVMOpCode.Execute_44_Operands:
                case ERigVMOpCode.Execute_45_Operands:
                case ERigVMOpCode.Execute_46_Operands:
                case ERigVMOpCode.Execute_47_Operands:
                case ERigVMOpCode.Execute_48_Operands:
                case ERigVMOpCode.Execute_49_Operands:
                case ERigVMOpCode.Execute_50_Operands:
                case ERigVMOpCode.Execute_51_Operands:
                case ERigVMOpCode.Execute_52_Operands:
                case ERigVMOpCode.Execute_53_Operands:
                case ERigVMOpCode.Execute_54_Operands:
                case ERigVMOpCode.Execute_55_Operands:
                case ERigVMOpCode.Execute_56_Operands:
                case ERigVMOpCode.Execute_57_Operands:
                case ERigVMOpCode.Execute_58_Operands:
                case ERigVMOpCode.Execute_59_Operands:
                case ERigVMOpCode.Execute_60_Operands:
                case ERigVMOpCode.Execute_61_Operands:
                case ERigVMOpCode.Execute_62_Operands:
                case ERigVMOpCode.Execute_63_Operands:
                case ERigVMOpCode.Execute_64_Operands:
                {
                    var op = Ar.Read<FRigVMExecuteOp>();
                    var count = op.OpCode - ERigVMOpCode.Execute_0_Operands;

                    for (var idx = 0; idx < count; idx++)
                    {
                        var _ = Ar.Read<FRigVMOperand>();
                    }

                    break;
                }
                case ERigVMOpCode.Copy:
                {
                    var _ = Ar.Read<FRigVMCopyOp>();
                    break;
                }
                case ERigVMOpCode.Zero:
                case ERigVMOpCode.BoolFalse:
                case ERigVMOpCode.BoolTrue:
                case ERigVMOpCode.Increment:
                case ERigVMOpCode.Decrement:
                {
                    var _ = Ar.Read<FRigVMUnaryOp>();
                    break;
                }
                case ERigVMOpCode.Equals:
                case ERigVMOpCode.NotEquals:
                {
                    var _ = Ar.Read<FRigVMComparisonOp>();
                    break;
                }
                case ERigVMOpCode.JumpAbsolute:
                case ERigVMOpCode.JumpForward:
                case ERigVMOpCode.JumpBackward:
                {
                    var _ = Ar.Read<FRigVMJumpOp>();
                    break;
                }
                case ERigVMOpCode.JumpAbsoluteIf:
                case ERigVMOpCode.JumpForwardIf:
                case ERigVMOpCode.JumpBackwardIf:
                {
                    var _ = Ar.Read<FRigVMJumpIfOp>();
                    break;
                }
                case ERigVMOpCode.BeginBlock:
                {
                    var _ = Ar.Read<FRigVMBinaryOp>();
                    break;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMExecuteOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly ushort FunctionIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMUnaryOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly FRigVMOperand Arg;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMOperand
    {
        public readonly ERigVMMemoryType MemoryType;
        public readonly ushort RegisterIndex;
        public readonly ushort RegisterOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMCopyOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly FRigVMOperand Source;
        public readonly FRigVMOperand Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMComparisonOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly FRigVMOperand A;
        public readonly FRigVMOperand B;
        public readonly FRigVMOperand Result;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMJumpOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly int InstructionIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMJumpIfOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly FRigVMOperand Arg;
        public readonly int InstructionIndex;
        public readonly bool Condition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FRigVMBinaryOp
    {
        public readonly ERigVMOpCode OpCode;
        public readonly FRigVMOperand ArgA;
        public readonly FRigVMOperand ArgB;
    }

    public enum ERigVMOpCode : byte
    {
        Execute_0_Operands, // execute a rig function with 0 operands
        Execute_1_Operands, // execute a rig function with 1 operands
        Execute_2_Operands, // execute a rig function with 2 operands
        Execute_3_Operands, // execute a rig function with 3 operands
        Execute_4_Operands, // execute a rig function with 4 operands
        Execute_5_Operands, // execute a rig function with 5 operands
        Execute_6_Operands, // execute a rig function with 6 operands
        Execute_7_Operands, // execute a rig function with 7 operands
        Execute_8_Operands, // execute a rig function with 8 operands
        Execute_9_Operands, // execute a rig function with 9 operands
        Execute_10_Operands, // execute a rig function with 10 operands
        Execute_11_Operands, // execute a rig function with 11 operands
        Execute_12_Operands, // execute a rig function with 12 operands
        Execute_13_Operands, // execute a rig function with 13 operands
        Execute_14_Operands, // execute a rig function with 14 operands
        Execute_15_Operands, // execute a rig function with 15 operands
        Execute_16_Operands, // execute a rig function with 16 operands
        Execute_17_Operands, // execute a rig function with 17 operands
        Execute_18_Operands, // execute a rig function with 18 operands
        Execute_19_Operands, // execute a rig function with 19 operands
        Execute_20_Operands, // execute a rig function with 20 operands
        Execute_21_Operands, // execute a rig function with 21 operands
        Execute_22_Operands, // execute a rig function with 22 operands
        Execute_23_Operands, // execute a rig function with 23 operands
        Execute_24_Operands, // execute a rig function with 24 operands
        Execute_25_Operands, // execute a rig function with 25 operands
        Execute_26_Operands, // execute a rig function with 26 operands
        Execute_27_Operands, // execute a rig function with 27 operands
        Execute_28_Operands, // execute a rig function with 28 operands
        Execute_29_Operands, // execute a rig function with 29 operands
        Execute_30_Operands, // execute a rig function with 30 operands
        Execute_31_Operands, // execute a rig function with 31 operands
        Execute_32_Operands, // execute a rig function with 32 operands
        Execute_33_Operands, // execute a rig function with 33 operands
        Execute_34_Operands, // execute a rig function with 34 operands
        Execute_35_Operands, // execute a rig function with 35 operands
        Execute_36_Operands, // execute a rig function with 36 operands
        Execute_37_Operands, // execute a rig function with 37 operands
        Execute_38_Operands, // execute a rig function with 38 operands
        Execute_39_Operands, // execute a rig function with 39 operands
        Execute_40_Operands, // execute a rig function with 40 operands
        Execute_41_Operands, // execute a rig function with 41 operands
        Execute_42_Operands, // execute a rig function with 42 operands
        Execute_43_Operands, // execute a rig function with 43 operands
        Execute_44_Operands, // execute a rig function with 44 operands
        Execute_45_Operands, // execute a rig function with 45 operands
        Execute_46_Operands, // execute a rig function with 46 operands
        Execute_47_Operands, // execute a rig function with 47 operands
        Execute_48_Operands, // execute a rig function with 48 operands
        Execute_49_Operands, // execute a rig function with 49 operands
        Execute_50_Operands, // execute a rig function with 50 operands
        Execute_51_Operands, // execute a rig function with 51 operands
        Execute_52_Operands, // execute a rig function with 52 operands
        Execute_53_Operands, // execute a rig function with 53 operands
        Execute_54_Operands, // execute a rig function with 54 operands
        Execute_55_Operands, // execute a rig function with 55 operands
        Execute_56_Operands, // execute a rig function with 56 operands
        Execute_57_Operands, // execute a rig function with 57 operands
        Execute_58_Operands, // execute a rig function with 58 operands
        Execute_59_Operands, // execute a rig function with 59 operands
        Execute_60_Operands, // execute a rig function with 60 operands
        Execute_61_Operands, // execute a rig function with 61 operands
        Execute_62_Operands, // execute a rig function with 62 operands
        Execute_63_Operands, // execute a rig function with 63 operands
        Execute_64_Operands, // execute a rig function with 64 operands
        Zero, // zero the memory of a given register
        BoolFalse, // set a given register to false
        BoolTrue, // set a given register to true
        Copy, // copy the content of one register to another
        Increment, // increment a int32 register
        Decrement, // decrement a int32 register
        Equals, // fill a bool register with the result of (A == B)
        NotEquals, // fill a bool register with the result of (A != B)
        JumpAbsolute, // jump to an absolute instruction index
        JumpForward, // jump forwards given a relative instruction index offset
        JumpBackward, // jump backwards given a relative instruction index offset
        JumpAbsoluteIf, // jump to an absolute instruction index based on a condition register
        JumpForwardIf, // jump forwards given a relative instruction index offset based on a condition register
        JumpBackwardIf, // jump backwards given a relative instruction index offset based on a condition register
        ChangeType, // change the type of a register
        Exit, // exit the execution loop
        BeginBlock, // begins a new memory slice / block
        EndBlock, // ends the last memory slice / block
        Invalid
    }
}