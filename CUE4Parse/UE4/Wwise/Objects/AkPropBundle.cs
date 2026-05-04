using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkPropBundle(FWwiseArchive Ar)
{
    public readonly AkProp[] Props = ReadSequentialAkProp(Ar);
    public readonly AkPropRange[] PropRanges = ReadSequentialAkPropRange(Ar);

    public static AkProp[] ReadSequentialAkProp(FWwiseArchive Ar)
    {
        int propCount = Ar.Read<byte>();
        var ids = Ar.ReadArray(propCount, Ar.Read<byte>);

        var props = new AkProp[propCount];
        for (int i = 0; i < propCount; i++)
            props[i] = new AkProp(ids[i], AkUnionValue.Read(Ar));

        return props;
    }

    public static AkPropRange[] ReadSequentialAkPropRange(FWwiseArchive Ar)
    {
        int propCount = Ar.Read<byte>();
        var ids = Ar.ReadArray(propCount, Ar.Read<byte>);

        var ranges = new AkPropRange[propCount];
        for (int i = 0; i < propCount; i++)
        {
            AkUnionValue min = AkUnionValue.Read(Ar);
            AkUnionValue max = AkUnionValue.Read(Ar);
            ranges[i] = new AkPropRange(ids[i], min, max);
        }

        return ranges;
    }
}

public readonly struct AkProp(byte id, AkUnionValue value)
{
    public readonly byte Id = id;
    public readonly AkUnionValue Value = value;
}

public readonly struct AkPropRange(byte id, AkUnionValue min, AkUnionValue max)
{
    public readonly byte Id = id;
    public readonly AkUnionValue Min = min;
    public readonly AkUnionValue Max = max;
}

[JsonConverter(typeof(AkPropValueConverter))]
[StructLayout(LayoutKind.Explicit)]
public struct AkUnionValue
{
    [FieldOffset(0)] public float f32;
    [FieldOffset(0)] public uint u32;
    // Guessing value type, in Wwise it's determined by the subclass, this is simply more convenient
    public readonly object Value => u32 > 0x10000000 ? f32 : u32;

    public AkUnionValue(uint val) : this() => u32 = val;

    public static AkUnionValue Read(FWwiseArchive Ar)
    {
        uint raw = Ar.Read<uint>();
        return new AkUnionValue(raw);
    }
}
