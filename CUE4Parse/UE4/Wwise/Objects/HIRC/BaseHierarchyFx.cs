using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyFx : AbstractHierarchy
{
    public readonly AkMediaMap[] MediaList;
    public readonly AkRtpc[] RTPCs;
    public readonly AkStateGroup[] StateGroups = [];
    public readonly RtpcInit[] RtpcInitList = [];
    public readonly PluginPropertyValue[] PluginPropertyValues = [];

    public BaseHierarchyFx(FArchive Ar) : base(Ar)
    {
        var pluginId = WwisePlugin.ParsePlugin(Ar);

        WwisePlugin.ParsePluginParams(Ar, pluginId);

        MediaList = Ar.ReadArray(Ar.Read<byte>(), () => new AkMediaMap(Ar));
        RTPCs = AkRtpc.ReadArray(Ar);

        if (WwiseVersions.Version <= 89)
        {
            // Do nothing for versions <= 89
        }
        else if (WwiseVersions.Version <= 126)
        {
            if (WwiseVersions.Version > 122)
            {
                // Unused bytes
                Ar.Read<byte>();
                Ar.Read<byte>();
            }

            RtpcInitList = Ar.ReadArray(Ar.Read<ushort>(), () => new RtpcInit(Ar));
        }
        else
        {
            StateGroups = new AkStateAwareChunk(Ar).Groups;
            PluginPropertyValues = Ar.ReadArray(Ar.Read<ushort>(), () => new PluginPropertyValue(Ar));
        }
    }

    public readonly struct RtpcInit
    {
        public readonly int ParamId;
        public readonly float InitValue;

        public RtpcInit(FArchive Ar)
        {
            ParamId = WwiseReader.Read7BitEncodedIntBE(Ar);
            InitValue = Ar.Read<float>();
        }
    }

    public readonly struct PluginPropertyValue
    {
        public readonly int PropertyId;
        public readonly byte RtpcAccum;
        public readonly float Value;

        public PluginPropertyValue(FArchive Ar)
        {
            PropertyId = WwiseReader.Read7BitEncodedIntBE(Ar);
            RtpcAccum = Ar.Read<byte>();
            Value = Ar.Read<float>();
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("MediaList");
        serializer.Serialize(writer, MediaList);

        writer.WritePropertyName("RtpcList");
        serializer.Serialize(writer, RTPCs);

        writer.WritePropertyName("StateGroups");
        serializer.Serialize(writer, StateGroups);

        writer.WritePropertyName("RtpcInitList");
        serializer.Serialize(writer, RtpcInitList);

        writer.WritePropertyName("PluginPropertyValues");
        serializer.Serialize(writer, PluginPropertyValues);
    }
}
