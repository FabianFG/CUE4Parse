using System.Runtime.InteropServices;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteConstants;
using static CUE4Parse.UE4.Assets.Exports.Nanite.NaniteUtils;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public readonly struct FUVRange
{
    public readonly TIntVector2<uint> Min;
    public readonly TIntVector2<uint> NumBits;
    public readonly uint NumMantissaBits;

    public FUVRange(FArchive Ar)
    {
        var packed = Ar.Read<TIntVector2<uint>>();
        Min = new TIntVector2<uint>(packed.X >> 5, packed.Y >> 5);
        NumBits = new TIntVector2<uint>(GetBits(packed.X, 5, 0), GetBits(packed.Y, 5, 0));
        NumMantissaBits = NANITE_UV_FLOAT_NUM_MANTISSA_BITS;
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
