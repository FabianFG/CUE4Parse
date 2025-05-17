using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyModulator : AbstractHierarchy
{
    public List<AkProp> Props { get; private set; } = [];
    public List<AkPropRange> PropRanges { get; private set; }
    public List<AkRTPC> RTPCs { get; private set; }

    public BaseHierarchyModulator(FArchive Ar) : base(Ar)
    {
        Props = AkPropBundle.ReadLinearAkProp(Ar);
        PropRanges = AkPropBundle.ReadLinearAkPropRange(Ar);
        RTPCs = AkRTPC.ReadMultiple(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("Props");
        serializer.Serialize(writer, Props);

        writer.WritePropertyName("PropRanges");
        serializer.Serialize(writer, PropRanges);

        writer.WritePropertyName("RTPCs");
        serializer.Serialize(writer, RTPCs);
    }
}
