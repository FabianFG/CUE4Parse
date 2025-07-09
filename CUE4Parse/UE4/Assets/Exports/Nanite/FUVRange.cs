using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteUtils;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public readonly struct FUVRange
{
    private static readonly Vector64<uint> _mask = (Vector64<uint>.One << 5) - Vector64<uint>.One;

    public readonly Vector64<uint> Min;
    public readonly Vector64<uint> NumBits;
    public readonly uint NumMantissaBits;
    public readonly uint TexCoordBytesPerValue;

    public FUVRange(FArchive Ar)
    {
        var packed = Ar.Read<Vector64<uint>>();
        Min = packed >> 5;
        NumBits = packed & _mask;
        NumMantissaBits = NANITE_UV_FLOAT_NUM_MANTISSA_BITS;
        TexCoordBytesPerValue = (Math.Max(NumBits[0], NumBits[1]) + 7) / 8;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 32)]
public readonly struct FUVRange_Old
{
    public readonly TIntVector2<int> Min;
    public readonly TIntVector2<uint> GapStart;
    public readonly TIntVector2<uint> GapLength;
    public readonly int Precision;
    [JsonIgnore]
    public readonly uint Padding;
}
