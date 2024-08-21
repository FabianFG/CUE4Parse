namespace CUE4Parse.UE4.Objects.RigVM;

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

    ArrayReset, // (DEPRECATED) clears an array and resets its content
    ArrayGetNum, // (DEPRECATED) reads and returns the size of an array (binary op, in array, out int32)
    ArraySetNum, // (DEPRECATED) resizes an array (binary op, in out array, in int32)
    ArrayGetAtIndex, // (DEPRECATED) returns an array element by index (ternary op, in array, in int32, out element)
    ArraySetAtIndex, // (DEPRECATED) sets an array element by index (ternary op, in out array, in int32, in element)
    ArrayAdd, // (DEPRECATED) adds an element to an array (ternary op, in out array, in element, out int32 index)
    ArrayInsert, // (DEPRECATED) inserts an element to an array (ternary op, in out array, in int32, in element)
    ArrayRemove, // (DEPRECATED) removes an element from an array (binary op, in out array, in inindex)
    ArrayFind, // (DEPRECATED) finds and returns the index of an element (quaternery op, in array, in element, out int32 index, out bool success)
    ArrayAppend, // (DEPRECATED) appends an array to another (binary op, in out array, in array)
    ArrayClone, // (DEPRECATED) clones an array (binary op, in array, out array)
    ArrayIterator, // (DEPRECATED) iterates over an array (senary op, in array, out element, out index, out count, out ratio, out continue)
    ArrayUnion, // (DEPRECATED) merges two arrays while avoiding duplicates (binary op, in out array, in other array)
    ArrayDifference, // (DEPRECATED) returns a new array containing elements only found in one array (ternary op, in array, in array, out result)
    ArrayIntersection, // (DEPRECATED) returns a new array containing elements found in both of the input arrays (ternary op, in array, in array, out result)
    ArrayReverse, // (DEPRECATED) returns the reverse of the input array (unary op, in out array)
    InvokeEntry, // invokes an entry from the entry list
    JumpToBranch, // jumps to a branch based on a name operand
    Execute, // single execute op (formerly Execute_0_Operands to Execute_64_Operands)
    RunInstructions, // runs a set of instructions lazily
    Invalid,
    FirstArrayOpCode = ArrayReset,
    LastArrayOpCode = ArrayReverse,
}
