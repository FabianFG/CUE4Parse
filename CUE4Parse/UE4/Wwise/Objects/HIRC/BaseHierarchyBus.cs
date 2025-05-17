using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyBus : AbstractHierarchy
{
    public uint OverrideBusId { get; private set; }
    public uint DeviceSharesetId { get; private set; }
    public List<AkProp> Props { get; private set; } = [];
    public AkPositioningParams? PositioningParams { get; private set; }
    public AkAuxParams? AuxParams { get; private set; }
    public EAdvSettings? AdvSettingsParams { get; private set; }
    public ushort? MaxNumInstance { get; private set; }
    public uint? ChannelConfig { get; private set; }
    public byte? HdrEnvelopeFlags { get; private set; }
    public uint RecoveryTime { get; private set; }
    public float MaxDuckVolume { get; private set; }
    public List<AkDuckInfo> DuckInfo { get; private set; } = [];
    public AkFXBus FXBusParams { get; private set; }
    public byte OverrideAttachmentParams { get; private set; }
    public List<AkFXChunk> FXChunk { get; private set; } = [];
    public List<AkRTPC> RTPCs { get; private set; }
    public List<AkStateGroup>? StateGroups { get; private set; }

    public BaseHierarchyBus(FArchive Ar) : base(Ar)
    {
        OverrideBusId = Ar.Read<uint>();
        if (WwiseVersions.Version > 126 && OverrideBusId == 0)
        {
            DeviceSharesetId = Ar.Read<uint>();
        }

        if (WwiseVersions.Version > 56)
        {
            int propCount = Ar.Read<byte>();
            var propIds = new List<byte>(propCount);
            var propValues = new List<float>(propCount);

            for (int i = 0; i < propCount; i++)
            {
                var propId = Ar.Read<byte>();
                propIds.Add(propId);
            }

            for (int i = 0; i < propCount; i++)
            {
                var propValue = Ar.Read<float>();
                propValues.Add(propValue);
            }

            Props = new List<AkProp>(propCount);
            for (int i = 0; i < propCount; i++)
            {
                Props.Add(new AkProp(propIds[i], propValues[i]));
            }
        }

        if (WwiseVersions.Version > 122)
        {
            PositioningParams = new AkPositioningParams(Ar);
            AuxParams = new AkAuxParams(Ar);
        }

        if (WwiseVersions.Version <= 53)
        {
            // TODO: Handle this case
        }
        else
        {
            AdvSettingsParams = Ar.Read<EAdvSettings>();
            MaxNumInstance = Ar.Read<ushort>();
            ChannelConfig = Ar.Read<uint>();
            HdrEnvelopeFlags = Ar.Read<byte>();
        }

        if (WwiseVersions.Version <= 56)
        {
            var stateGroupId = Ar.Read<uint>();
        }

        RecoveryTime = Ar.Read<uint>();

        if (WwiseVersions.Version > 38)
        {
            MaxDuckVolume = Ar.Read<float>();
        }

        if (WwiseVersions.Version <= 56)
        {
            var stateSyncType = Ar.Read<uint>();
        }

        var numDucks = Ar.Read<uint>();
        for (int i = 0; i < numDucks; i++)
        {
            DuckInfo.Add(new AkDuckInfo(Ar));
        }

        FXBusParams = new AkFXBus(Ar);

        if (WwiseVersions.Version > 89 && WwiseVersions.Version <= 145)
        {
            OverrideAttachmentParams = Ar.Read<byte>();
        }

        if (WwiseVersions.Version > 136)
        {
            var numFx = Ar.Read<byte>();
            if (numFx > 0)
            {
                for (int i = 0; i < numFx; i++)
                {
                    FXChunk.Add(new AkFXChunk(Ar));
                }
            }
        }

        RTPCs = AkRTPC.ReadMultiple(Ar);

        if (WwiseVersions.Version <= 56)
        {
            // TODO: State chunk inlined
        }
        else
        {
            StateGroups = new AkStateChunk(Ar).Groups;
        }

        if (WwiseVersions.Version <= 126)
        {
            // TODO: FeedbackInfo
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("OverrideBusId");
        writer.WriteValue(OverrideBusId);
        writer.WritePropertyName("DeviceSharesetId");
        writer.WriteValue(DeviceSharesetId);

        writer.WritePropertyName("Props");
        writer.WriteStartArray();
        foreach (var p in Props)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Id");
            writer.WriteValue(p.Id);
            writer.WritePropertyName("Value");
            writer.WriteValue(p.Value);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        if (PositioningParams != null)
        {
            writer.WritePropertyName("PositioningParams");
            serializer.Serialize(writer, PositioningParams);
        }

        if (AuxParams != null)
        {
            writer.WritePropertyName("AuxParams");
            serializer.Serialize(writer, AuxParams);
        }

        if (AdvSettingsParams.HasValue)
        {
            writer.WritePropertyName("AdvSettingsParams");
            writer.WriteValue(AdvSettingsParams.Value.ToString());
        }

        if (MaxNumInstance.HasValue)
        {
            writer.WritePropertyName("MaxNumInstance");
            writer.WriteValue(MaxNumInstance.Value);
        }

        if (ChannelConfig.HasValue)
        {
            writer.WritePropertyName("ChannelConfig");
            writer.WriteValue(ChannelConfig.Value);
        }

        if (HdrEnvelopeFlags.HasValue)
        {
            writer.WritePropertyName("HdrEnvelopeFlags");
            writer.WriteValue(HdrEnvelopeFlags.Value);
        }

        writer.WritePropertyName("RecoveryTime");
        writer.WriteValue(RecoveryTime);

        writer.WritePropertyName("DuckInfo");
        writer.WriteStartArray();
        foreach (var d in DuckInfo)
            serializer.Serialize(writer, d);
        writer.WriteEndArray();

        writer.WritePropertyName("FXBusParams");
        serializer.Serialize(writer, FXBusParams);

        writer.WritePropertyName("OverrideAttachmentParams");
        writer.WriteValue(OverrideAttachmentParams);

        writer.WritePropertyName("FXChunk");
        writer.WriteStartArray();
        foreach (var f in FXChunk)
            serializer.Serialize(writer, f);
        writer.WriteEndArray();

        writer.WritePropertyName("RTPCs");
        writer.WriteStartArray();
        foreach (var r in RTPCs)
            serializer.Serialize(writer, r);
        writer.WriteEndArray();

        if (StateGroups != null)
        {
            writer.WritePropertyName("StateGroups");
            serializer.Serialize(writer, StateGroups);
        }
    }
}
