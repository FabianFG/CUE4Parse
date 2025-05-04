using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkProp
{
    public byte ID { get; }
    public float Value { get; }

    public AkProp(FArchive ar)
    {
        ID = ar.Read<byte>();
        Value = ar.Read<float>();
    }
}

public class AkPropRange
{
    public byte ID { get; }
    public float Min { get; }
    public float Max { get; }

    public AkPropRange(FArchive ar)
    {
        ID = ar.Read<byte>();
        Min = ar.Read<float>();
        Max = ar.Read<float>();
    }
}

public class AkPropBundle
{
    public List<AkProp> Props { get; }
    public List<AkPropRange> PropRanges { get; }

    public AkPropBundle(FArchive ar)
    {
        int propCount = ar.Read<byte>();
        Props = new List<AkProp>(propCount);
        for (int i = 0; i < propCount; i++)
        {
            Props.Add(new AkProp(ar));
        }

        int rangeCount = ar.Read<byte>();
        PropRanges = new List<AkPropRange>(rangeCount);
        for (int i = 0; i < rangeCount; i++)
        {
            PropRanges.Add(new AkPropRange(ar));
        }
    }
}
