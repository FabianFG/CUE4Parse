using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

[StructLayout(LayoutKind.Explicit)]
public struct FArgs
{
    [FieldOffset(0)] public int IntConstantArgs;
    [FieldOffset(0)] public float ScalarConstantArgs;
    [FieldOffset(0)] public ColourConstantArgs ColourConstantArgs;
    
}

[InlineArray(4)]
public struct ColourConstantArgs
{
    // float value[4]
    private float _element0;

    public Span<float> Get()
    {
        return MemoryMarshal.CreateSpan(ref _element0, 4);
    }
}