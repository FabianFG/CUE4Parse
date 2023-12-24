using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

[StructLayout(LayoutKind.Explicit)]
public struct BoolConstantArgs
{
    [FieldOffset(0)] public bool Value;
}

[StructLayout(LayoutKind.Explicit)]
public struct IntConstantArgs
{
    [FieldOffset(0)] public int Value;
}

[StructLayout(LayoutKind.Explicit)]
public struct ScalarConstantArgs
{
    [FieldOffset(0)] public float Value;
}

[StructLayout(LayoutKind.Explicit)]
public struct ColourConstantArgs
{
    // float value[4] ?
    [FieldOffset(0)] public float Value0;
    [FieldOffset(0)] public float Value1;
    [FieldOffset(0)] public float Value2;
    [FieldOffset(0)] public float Value3;
}