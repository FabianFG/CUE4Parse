using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkPropBundle
{
    public readonly List<AkProp> Props;
    public readonly List<AkPropRange> PropRanges;

    public AkPropBundle(FArchive Ar)
    {
        int propCount = Ar.Read<byte>();
        Props = new List<AkProp>(propCount);
        for (int i = 0; i < propCount; i++)
        {
            Props.Add(new AkProp(Ar));
        }

        int rangeCount = Ar.Read<byte>();
        PropRanges = new List<AkPropRange>(rangeCount);
        for (int i = 0; i < rangeCount; i++)
        {
            PropRanges.Add(new AkPropRange(Ar));
        }
    }

    public static List<AkProp> ReadLinearAkProp(FArchive Ar)
    {
        int propCount = Ar.Read<byte>();
        var ids = new List<byte>(propCount);
        var values = new List<float>(propCount);

        for (int i = 0; i < propCount; i++)
        {
            ids.Add(Ar.Read<byte>());
        }

        for (int i = 0; i < propCount; i++)
        {
            values.Add(Ar.Read<float>());
        }

        List<AkProp> props = new(propCount);
        for (int i = 0; i < propCount; i++)
        {
            props.Add(new AkProp(ids[i], values[i]));
        }

        return props;
    }

    public static List<AkPropRange> ReadLinearAkPropRange(FArchive Ar)
    {
        int propCount = Ar.Read<byte>();
        var ids = new List<byte>(propCount);

        for (int i = 0; i < propCount; i++)
        {
            ids.Add(Ar.Read<byte>());
        }

        var ranges = new List<AkPropRange>(propCount);
        for (int i = 0; i < propCount; i++)
        {
            float min = Ar.Read<float>();
            float max = Ar.Read<float>();
            ranges.Add(new AkPropRange(ids[i], min, max));
        }

        return ranges;
    }
}

public class AkProp
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

public class AkPropRange
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
