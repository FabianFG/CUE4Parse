using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyFx : AbstractHierarchy
{
    public class RTPCInit
    {
        public byte ParamID { get; set; }
        public float InitValue { get; set; }
    }

    public class PluginPropertyValue
    {
        public byte PropertyId { get; set; }
        public byte RtpcAccum { get; set; }
        public float Value { get; set; }
    }

    public List<AkRTPC> RTPCs { get; protected set; }
    public List<AkStateGroup>? StateGroups { get; protected set; }

    public BaseHierarchyFx(FArchive Ar) : base(Ar)
    {
        var pluginId = WwisePlugin.ParsePlugin(Ar);

        WwisePlugin.ParsePluginParams(Ar, pluginId);

        var numBankData = Ar.Read<byte>();
        var mediaList = new List<AkMediaMap>(numBankData);
        for (int i = 0; i < numBankData; i++)
        {
            var mediaItem = new AkMediaMap
            {
                Index = Ar.Read<byte>(),
                SourceId = Ar.Read<uint>()
            };
            mediaList.Add(mediaItem);
        }

        RTPCs = AkRTPC.ReadMultiple(Ar);

        if (WwiseVersions.WwiseVersion <= 89)
        {
            // Do nothing for versions <= 89
        }
        else if (WwiseVersions.WwiseVersion <= 126)
        {
            if (WwiseVersions.WwiseVersion > 122)
            {
                // Unused bytes
                Ar.Read<byte>();
                Ar.Read<byte>();
            }

            var numInit = Ar.Read<ushort>();
            var rtpcInitList = new List<RTPCInit>(numInit);
            for (int i = 0; i < numInit; i++)
            {
                var rtpcInit = new RTPCInit
                {
                    ParamID = Ar.Read<byte>(),
                    InitValue = Ar.Read<float>()
                };
                rtpcInitList.Add(rtpcInit);
            }
        }
        else
        {
            StateGroups = new AkStateChunk(Ar).Groups;

            var numValues = Ar.Read<ushort>();
            var propertyValuesList = new List<PluginPropertyValue>(numValues);
            for (int i = 0; i < numValues; i++)
            {
                var propertyValue = new PluginPropertyValue
                {
                    PropertyId = Ar.Read<byte>(),
                    RtpcAccum = Ar.Read<byte>(),
                    Value = Ar.Read<float>()
                };
                propertyValuesList.Add(propertyValue);
            }
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}
