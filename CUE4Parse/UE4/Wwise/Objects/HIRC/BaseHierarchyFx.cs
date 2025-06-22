using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyFx : AbstractHierarchy
{
    public readonly List<AkMediaMap> MediaList;
    public readonly List<AkRtpc> RtpcList;
    public readonly List<AkStateGroup> StateGroups = [];
    public readonly List<RtpcInit> RtpcInitList = [];
    public readonly List<PluginPropertyValue> PluginPropertyValues = [];

    public BaseHierarchyFx(FArchive Ar) : base(Ar)
    {
        var pluginId = WwisePlugin.ParsePlugin(Ar);

        WwisePlugin.ParsePluginParams(Ar, pluginId);

        var numBankData = Ar.Read<byte>();
        MediaList = new List<AkMediaMap>(numBankData);
        for (int i = 0; i < numBankData; i++)
        {
            var mediaItem = new AkMediaMap
            {
                Index = Ar.Read<byte>(),
                SourceId = Ar.Read<uint>()
            };
            MediaList.Add(mediaItem);
        }

        RtpcList = AkRtpc.ReadMultiple(Ar);

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

            var numInit = Ar.Read<ushort>();
            RtpcInitList = new List<RtpcInit>(numInit);
            for (int i = 0; i < numInit; i++)
            {
                RtpcInitList.Add(new RtpcInit
                {
                    ParamId = WwiseReader.Read7BitEncodedIntBE(Ar),
                    InitValue = Ar.Read<float>()
                });
            }
        }
        else
        {
            StateGroups = new AkStateAwareChunk(Ar).Groups;

            var numValues = Ar.Read<ushort>();
            PluginPropertyValues = new List<PluginPropertyValue>(numValues);
            for (int i = 0; i < numValues; i++)
            {
                PluginPropertyValues.Add(new PluginPropertyValue
                {
                    PropertyId = WwiseReader.Read7BitEncodedIntBE(Ar),
                    RtpcAccum = Ar.Read<byte>(),
                    Value = Ar.Read<float>()
                });
            }
        }
    }

    public class RtpcInit
    {
        public int ParamId { get; set; }
        public float InitValue { get; set; }
    }

    public class PluginPropertyValue
    {
        public int PropertyId { get; set; }
        public byte RtpcAccum { get; set; }
        public float Value { get; set; }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("MediaList");
        serializer.Serialize(writer, MediaList);

        writer.WritePropertyName("RtpcList");
        serializer.Serialize(writer, RtpcList);

        writer.WritePropertyName("StateGroups");
        serializer.Serialize(writer, StateGroups);

        writer.WritePropertyName("RtpcInitList");
        serializer.Serialize(writer, RtpcInitList);

        writer.WritePropertyName("PluginPropertyValues");
        serializer.Serialize(writer, PluginPropertyValues);
    }
}
