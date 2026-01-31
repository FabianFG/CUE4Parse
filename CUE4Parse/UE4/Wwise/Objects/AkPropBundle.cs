using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public readonly struct AkPropBundle
{
    public readonly AkProp[] Props;
    public readonly AkPropRange[] PropRanges;

    public AkPropBundle(FArchive Ar)
    {
        Props = Ar.ReadArray(Ar.Read<byte>(), () => new AkProp(Ar));
        PropRanges = Ar.ReadArray(Ar.Read<byte>(), () => new AkPropRange(Ar));
    }

    public static AkProp[] ReadSequentialAkProp(FArchive Ar)
    {
        int propCount = Ar.Read<byte>();

        var ids = Ar.ReadArray(propCount, Ar.Read<byte>);
        var values = Ar.ReadArray(propCount, Ar.Read<float>);

        var props = new AkProp[propCount];
        for (int i = 0; i < propCount; i++)
            props[i] = new AkProp(ids[i], values[i]);

        return props;
    }

    public static AkPropRange[] ReadSequentialAkPropRange(FArchive Ar)
    {
        int propCount = Ar.Read<byte>();

        var ids = Ar.ReadArray(propCount, Ar.Read<byte>);

        var ranges = new AkPropRange[propCount];
        for (int i = 0; i < propCount; i++)
        {
            float min = Ar.Read<float>();
            float max = Ar.Read<float>();
            ranges[i] = new AkPropRange(ids[i], min, max);
        }

        return ranges;
    }
}

public readonly struct AkProp
{
    public readonly byte Id;
    public readonly float Value;

    public AkProp(FArchive Ar)
    {
        Id = Ar.Read<byte>();
        Value = Ar.Read<float>();
    }

    public AkProp(byte id, float value)
    {
        Id = id;
        Value = value;
    }
}

public readonly struct AkPropRange
{
    public readonly byte Id;
    public readonly float Min;
    public readonly float Max;

    public AkPropRange(FArchive Ar)
    {
        Id = Ar.Read<byte>();
        Min = Ar.Read<float>();
        Max = Ar.Read<float>();
    }

    public AkPropRange(byte id, float min, float max)
    {
        Id = id;
        Min = min;
        Max = max;
    }
}
