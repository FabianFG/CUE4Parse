using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyModulator : AbstractHierarchy
{
    public readonly AkProp[] Props = [];
    public readonly AkPropRange[] PropRanges;
    public readonly AkRtpc[] RtpcList;

    public BaseHierarchyModulator(FArchive Ar) : base(Ar)
    {
        Props = AkPropBundle.ReadLinearAkProp(Ar);
        PropRanges = AkPropBundle.ReadLinearAkPropRange(Ar);
        RtpcList = AkRtpc.ReadArray(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("Props");
        serializer.Serialize(writer, Props);

        writer.WritePropertyName("PropRanges");
        serializer.Serialize(writer, PropRanges);

        writer.WritePropertyName("RtpcList");
        serializer.Serialize(writer, RtpcList);
    }
}
