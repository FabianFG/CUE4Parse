using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkProp
{
    public byte Id { get; }
    public float Value { get; }

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
    public byte Id { get; }
    public float Min { get; }
    public float Max { get; }

    public AkPropRange(FArchive ar)
    {
        Id = ar.Read<byte>();
        Min = ar.Read<float>();
        Max = ar.Read<float>();
    }
}

public class AkPropBundle
{
    public List<AkProp> Props { get; }
    public List<AkPropRange> PropRanges { get; }

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
}
