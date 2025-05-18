using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyFx : AbstractHierarchy
{
    public class RTPCInit
    {
        public byte ParamId { get; set; }
        public float InitValue { get; set; }
    }

    public class PluginPropertyValue
    {
        public byte PropertyId { get; set; }
        public byte RtpcAccum { get; set; }
        public float Value { get; set; }
    }

    public readonly List<AkMediaMap> MediaList;
    public readonly List<AkRTPC> RTPCs;
    public readonly List<AkStateGroup> StateGroups = [];
    public readonly List<RTPCInit> RTPCInits = [];
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

        RTPCs = AkRTPC.ReadMultiple(Ar);

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
            RTPCInits = new List<RTPCInit>(numInit);
            for (int i = 0; i < numInit; i++)
            {
                RTPCInits.Add(new RTPCInit
                {
                    ParamId = Ar.Read<byte>(),
                    InitValue = Ar.Read<float>()
                });
            }
        }
        else
        {
            StateGroups = new AkStateChunk(Ar).Groups;

            var numValues = Ar.Read<ushort>();
            PluginPropertyValues = new List<PluginPropertyValue>(numValues);
            for (int i = 0; i < numValues; i++)
            {
                PluginPropertyValues.Add(new PluginPropertyValue
                {
                    PropertyId = Ar.Read<byte>(),
                    RtpcAccum = Ar.Read<byte>(),
                    Value = Ar.Read<float>()
                });
            }
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("RTPCs");
        serializer.Serialize(writer, RTPCs);

        writer.WritePropertyName("StateGroups");
        serializer.Serialize(writer, StateGroups);

        writer.WritePropertyName("RTPCInits");
        serializer.Serialize(writer, RTPCInits);

        writer.WritePropertyName("PluginPropertyValues");
        serializer.Serialize(writer, PluginPropertyValues);
    }
}
