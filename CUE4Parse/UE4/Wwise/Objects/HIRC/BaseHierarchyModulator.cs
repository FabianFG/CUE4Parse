using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

// CAkModulator
public class BaseHierarchyModulator : AbstractHierarchy
{
    public readonly AkProp[] Props = [];
    public readonly AkPropRange[] PropRanges;
    public readonly AkRtpc[] RtpcCurves;

    // CAkModulator::SetInitialValues
    public BaseHierarchyModulator(FArchive Ar) : base(Ar)
    {
        Props = AkPropBundle.ReadSequentialAkProp(Ar);
        PropRanges = AkPropBundle.ReadSequentialAkPropRange(Ar);
        RtpcCurves = AkRtpc.ReadArray(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName(nameof(Props));
        serializer.Serialize(writer, Props);

        writer.WritePropertyName(nameof(PropRanges));
        serializer.Serialize(writer, PropRanges);

        writer.WritePropertyName(nameof(RtpcCurves));
        serializer.Serialize(writer, RtpcCurves);
    }
}
